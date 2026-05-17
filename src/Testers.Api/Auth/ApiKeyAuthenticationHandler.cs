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

// Read header, SHA-256 hash, look up in api_key table (unique index = sub-ms PK lookup).
// Reject if revoked/expired. Emit ClaimsPrincipal with kind=Service so HttpContextCurrentUser
// can branch. LastUsedAt stamped fire-and-forget so auth latency isn't bottlenecked on a DB write.
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
            return AuthenticateResult.NoResult();   // let next scheme try

        var raw = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(raw)) return AuthenticateResult.NoResult();

        var hash = ApiKeyHasher.Hash(raw);
        var key = await _db.ApiKeys.AsNoTracking().FirstOrDefaultAsync(k => k.KeyHash == hash);

        if (key is null) return AuthenticateResult.Fail("Unknown API key.");
        if (!key.IsActive(_clock.UtcNow)) return AuthenticateResult.Fail("API key is revoked or expired.");

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
            claims.Add(new Claim(ClaimTypes.Role, scope));

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name);

        // Fire-and-forget; failures logged, never block auth.
        _ = UpdateLastUsedAsync(key.Id);

        return AuthenticateResult.Success(ticket);
    }

    private async Task UpdateLastUsedAsync(Guid keyId)
    {
        try
        {
            var tracked = await _db.ApiKeys.FindAsync(keyId);
            if (tracked is null) return;
            tracked.MarkUsed(_clock.UtcNow);
            await _db.SaveChangesAsync();
        }
#pragma warning disable CA1031
        catch (Exception ex) { Logger.LogWarning(ex, "Failed to stamp LastUsedAt on ApiKey {KeyId}", keyId); }
#pragma warning restore CA1031
    }
}
