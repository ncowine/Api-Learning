using Testers.Application.Abstractions;

namespace Testers.Infrastructure;

/// <summary>
/// Fallback <see cref="ICurrentUser"/> for contexts where no HTTP request is present
/// (background services, console-style entry points, integration test bootstrap before auth runs).
/// The Api project registers an HttpContext-aware implementation that supersedes this for
/// real requests via last-registration-wins.
/// </summary>
internal sealed class SystemCurrentUser : ICurrentUser
{
    private static readonly HashSet<string> EmptyRoles = new();

    public string Id => "system";

    public string DisplayName => "System";

    public UserKind Kind => UserKind.Service;

    public IReadOnlySet<string> Roles => EmptyRoles;

    public bool IsAuthenticated => true;
}
