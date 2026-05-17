namespace Testers.Infrastructure.Audit;

/// <summary>
/// One row per audited mutation. Currently the AuditInterceptor only stamps audit fields on
/// entities; AuditLog *writes* are a follow-up (handled by a deferred interceptor in a later
/// sub-step). The entity exists now so EF can build the table and migrations.
///
/// Lives in the App DB only (the user's responsibility, not the shared TestPlan DB).
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; private set; }

    /// <summary>FQ type name of the entity, e.g. <c>"Testers.Domain.Execution.TaskRun"</c>.</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>String-encoded primary key of the affected row.</summary>
    public string EntityKey { get; private set; } = string.Empty;

    /// <summary>"Created", "Modified", or "Deleted".</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Entity state snapshot at the moment of the action, as JSON. Nullable for tiny rows.</summary>
    public string? StateJson { get; private set; }

    /// <summary>ICurrentUser.Id at the moment of the action.</summary>
    public string UserId { get; private set; } = string.Empty;

    public string UserDisplayName { get; private set; } = string.Empty;

    public DateTime OccurredAt { get; private set; }

    public string? CorrelationId { get; private set; }

    private AuditLog()
    {
        // EF Core materialisation only.
    }

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
