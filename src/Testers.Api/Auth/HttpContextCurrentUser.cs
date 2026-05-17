using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Testers.Application.Abstractions;

namespace Testers.Api.Auth;

// Projects HttpContext.User into ICurrentUser regardless of which scheme authenticated.
// Falls back to "anonymous" if no auth ran, "system" if there's no HttpContext at all.
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
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.Ordinal)
        ?? new HashSet<string>();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
}
