using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Testers.Application.Abstractions;

namespace Testers.Api.Auth;

/// <summary>
/// HTTP-aware <see cref="ICurrentUser"/>: projects whichever scheme authenticated the request
/// (Okta JWT or API key) into the same flat interface. Handlers / interceptors never branch on
/// "is this a human or a service?" — they just consume <see cref="ICurrentUser.Kind"/>,
/// <see cref="ICurrentUser.Id"/>, <see cref="ICurrentUser.Roles"/>.
///
/// Falls back to <c>"anonymous"</c> if no authentication ran on the request (which should not
/// happen for endpoints behind <c>RequireAuthorization()</c>). Falls back to <c>"system"</c>
/// when there's no HttpContext at all (background services use the
/// <see cref="Testers.Infrastructure.SystemCurrentUser"/> registration instead, but this is
/// the belt-and-braces guard).
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public string Id =>
        Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Principal?.FindFirst("sub")?.Value
        ?? (_accessor.HttpContext is null ? "system" : "anonymous");

    public string DisplayName =>
        Principal?.FindFirst(ClaimTypes.Name)?.Value
        ?? Principal?.FindFirst("name")?.Value
        ?? Id;

    public UserKind Kind
    {
        get
        {
            var kindClaim = Principal?.FindFirst(ApiKeyAuthenticationHandler.ClaimTypeKind)?.Value;
            return kindClaim is not null && Enum.TryParse<UserKind>(kindClaim, out var parsed)
                ? parsed
                : UserKind.Human;
        }
    }

    public IReadOnlySet<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal)
        ?? new HashSet<string>();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
}
