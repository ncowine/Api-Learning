using Testers.Application.Abstractions;

namespace Testers.Infrastructure;

// Fallback for non-HTTP scopes (background services, test bootstrap). Api project overrides
// with the HttpContext-aware impl for real requests.
internal sealed class SystemCurrentUser : ICurrentUser
{
    private static readonly HashSet<string> EmptyRoles = new();

    public string Id => "system";
    public string DisplayName => "System";
    public UserKind Kind => UserKind.Service;
    public IReadOnlySet<string> Roles => EmptyRoles;
    public bool IsAuthenticated => true;
}
