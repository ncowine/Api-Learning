namespace Testers.Infrastructure.Auth;

// Service-account key. Raw value shown to admin once at creation; only the SHA-256 hash is stored.
// Auth handler hashes the X-Api-Key header and looks up KeyHash (unique index).
public sealed class ApiKey
{
    public Guid Id { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;   // SHA-256 hex, uppercase
    public string Prefix { get; private set; } = string.Empty;    // first 8 chars of raw, for display
    public string OwnerId { get; private set; } = string.Empty;
    public IReadOnlyList<string> Scopes { get; private set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private ApiKey() { }  // EF

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

    public void Revoke(DateTime now) => RevokedAt = now;
    public void MarkUsed(DateTime now) => LastUsedAt = now;
}
