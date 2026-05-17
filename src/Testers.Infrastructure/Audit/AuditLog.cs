namespace Testers.Infrastructure.Audit;

// One row per audited mutation. Currently only the table exists - interceptor writes are a
// follow-up (needs cross-DB plumbing decisions). Lives in the App DB.
public sealed class AuditLog
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string EntityKey { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;  // Created | Modified | Deleted
    public string? StateJson { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string UserDisplayName { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public string? CorrelationId { get; private set; }

    private AuditLog() { }  // EF

    internal AuditLog(
        string entityType,
        string entityKey,
        string action,
        string? stateJson,
        string userId,
        string userDisplayName,
        DateTime occurredAt,
        string? correlationId)
    {
        Id = Guid.NewGuid();
        EntityType = entityType;
        EntityKey = entityKey;
        Action = action;
        StateJson = stateJson;
        UserId = userId;
        UserDisplayName = userDisplayName;
        OccurredAt = occurredAt;
        CorrelationId = correlationId;
    }
}
