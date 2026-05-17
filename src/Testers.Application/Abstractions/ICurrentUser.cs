namespace Testers.Application.Abstractions;

public enum UserKind { Human, Service }

// HTTP impl in Api project reads HttpContext.User; background services fall back to "system".
public interface ICurrentUser
{
    string Id { get; }              // okta sub, "apikey:{guid}", or "system"
    string DisplayName { get; }
    UserKind Kind { get; }
    IReadOnlySet<string> Roles { get; }
    bool IsAuthenticated { get; }
}
