using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testers.Application.Abstractions;
using Testers.Infrastructure.Auth;
using Testers.Infrastructure.Persistence;

namespace Testers.Api.Auth;

/// <summary>
/// Custom <see cref="AuthenticationHandler{TOptions}"/> for service-account auth via API key.
///
/// Flow:
/// <list type="number">
///   <item>Read <c>X-Api-Key</c> header. If absent, return <see cref="AuthenticateResult.NoResult"/>
///         (lets the next scheme try; doesn't short-circuit).</item>
///   <item>Hash the raw key with SHA-256 (matching what <see cref="ApiKeyHasher"/> stores).</item>
///   <item>Look up by hash in <c>AppDb.ApiKeys</c>. Unique index makes this a sub-ms PK lookup.</item>
///   <item>Reject if revoked or expired. Otherwise emit a <see cref="ClaimsPrincipal"/> with
///         the API key's owner, scopes, and a Kind=Service claim that <c>HttpContextCurrentUser</c>
///         reads.</item>
///   <item>Stamp <c>LastUsedAt</c> as a fire-and-forget side effect (don't block auth on its commit).</item>
/// </list>
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public const string ClaimTypeOwnerId = "apikey:owner";
    public const string ClaimTypeKeyId = "apikey:id";
    public const string ClaimTypeKeyLabel = "apikey:label";
    public const string ClaimTypeKind = "user:kind";

    private readonly AppDbContext _db;
    private readonly IClock _clock;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext db,
        IClock clock)
        : base(options, logger, encoder)
    {
        _db = db;
        _clock = clock;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var raw = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return AuthenticateResult.NoResult();
        }

        var hash = ApiKeyHasher.Hash(raw);
        var key = await _db.ApiKeys.AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == hash)
            .ConfigureAwait(false);

        if (key is null)
        {
            return AuthenticateResult.Fail("Unknown API key.");
        }

        if (!key.IsActive(_clock.UtcNow))
        {
            return AuthenticateResult.Fail("API key is revoked or expired.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, $"apikey:{key.Id}"),
            new(ClaimTypes.Name, key.Label),
            new(ClaimTypeKeyId, key.Id.ToString()),
            new(ClaimTypeKeyLabel, key.Label),
            new(ClaimTypeOwnerId, key.OwnerId),
            new(ClaimTypeKind, UserKind.Service.ToString()),
        };
        foreach (var scope in key.Scopes)
        {
            claims.Add(new Claim(ClaimTypes.Role, scope));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // Fire-and-forget LastUsedAt update so auth latency isn't bottlenecked on a DB write.
        // Errors are logged but don't fail the request.
        _ = UpdateLastUsedAsync(key.Id);

        return AuthenticateResult.Success(ticket);
    }

    private async Task UpdateLastUsedAsync(Guid keyId)
    {
        try
        {
            var tracked = await _db.ApiKeys.FindAsync(keyId).ConfigureAwait(false);
            if (tracked is null)
            {
                return;
            }

            tracked.MarkUsed(_clock.UtcNow);
            await _db.SaveChangesAsync().ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to stamp LastUsedAt on ApiKey {KeyId}", keyId);
        }
#pragma warning restore CA1031
    }
}
