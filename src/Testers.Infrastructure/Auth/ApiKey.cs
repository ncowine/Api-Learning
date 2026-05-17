namespace Testers.Infrastructure.Auth;

/// <summary>
/// A service-account API key for non-human callers (CI pipelines, cron jobs, partner services).
///
/// Stored in the App DB. The raw key string is shown to the issuing admin exactly ONCE at
/// generation; only the SHA-256 hash is persisted. Auth handler (Api project) hashes the
/// incoming <c>X-Api-Key</c> header and looks up by <see cref="KeyHash"/>.
///
/// Lifecycle: created (active) → optionally expires (<see cref="ExpiresAt"/>) → or revoked
/// (<see cref="RevokedAt"/>). <see cref="IsActive"/> encapsulates both checks.
/// </summary>
public sealed class ApiKey
{
    public Guid Id { get; private set; }

    /// <summary>Human-readable label for the admin UI (e.g. "CI pipeline - main branch").</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>SHA-256 hex (uppercase). Unique index in the DB; primary lookup target.</summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>First few chars of the raw key, for display in the admin UI without leaking the secret.</summary>
    public string Prefix { get; private set; } = string.Empty;

    /// <summary>Free-form owner identifier (Okta user id, team id, service name).</summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>Scopes (roles) granted by this key. Projected through ICurrentUser.Roles.</summary>
    public IReadOnlyList<string> Scopes { get; private set; } = Array.Empty<string>();

    public DateTime CreatedAt { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public DateTime? LastUsedAt { get; private set; }

    private ApiKey()
    {
        // EF Core materialisation only.
    }

    internal ApiKey(
        string label,
        string keyHash,
        string prefix,
        string ownerId,
        IEnumerable<string> scopes,
        DateTime createdAt,
        DateTime? expiresAt)
    {
        Id = Guid.NewGuid();
        Label = label;
        KeyHash = keyHash;
        Prefix = prefix;
        OwnerId = ownerId;
        Scopes = scopes.ToList();
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public bool IsActive(DateTime now) =>
        RevokedAt is null && (ExpiresAt is null || ExpiresAt > now);

    internal void Revoke(DateTime now) => RevokedAt = now;

    internal void MarkUsed(DateTime now) => LastUsedAt = now;
}
