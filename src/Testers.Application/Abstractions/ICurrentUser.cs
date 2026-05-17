namespace Testers.Application.Abstractions;

/// <summary>Distinguishes a human (Okta JWT) from a service (DB-backed API key) caller.</summary>
public enum UserKind
{
    Human,
    Service,
}

/// <summary>
/// The identity of the caller making the current request. Populated from <c>HttpContext.User</c>
/// by an HTTP-aware implementation in the Api project. The AuditInterceptor consults this for
/// <c>CreatedBy</c>/<c>ModifiedBy</c> stamps. Background services (no HTTP user) get a default
/// service identity, e.g. <c>"system:outbox-publisher"</c>.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Okta <c>sub</c> (human) or <c>"apikey:{keyId}"</c> / <c>"system:{component}"</c> (service).</summary>
    string Id { get; }

    /// <summary>Friendly name used in audit logs and UI — full name for humans, key label for services.</summary>
    string DisplayName { get; }

    UserKind Kind { get; }

    /// <summary>Roles from Okta group claims (human) or the API key's scope list (service).</summary>
    IReadOnlySet<string> Roles { get; }

    bool IsAuthenticated { get; }
}
