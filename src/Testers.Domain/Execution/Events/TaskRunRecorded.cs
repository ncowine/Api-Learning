using Testers.Domain.Abstractions;

namespace Testers.Domain.Execution.Events;

/// <summary>
/// Raised when a tester records an action against a task on a specific build (a new TaskRun is
/// born). Captured by the OutboxInterceptor at <c>SaveChangesAsync</c> and published to RabbitMQ
/// with routing key <c>task.run.recorded.v1</c> (built from the type name by the interceptor's
/// convention).
///
/// Downstream consumers: read-model projections (status-per-build dashboards), notifications
/// (Slack on FAIL), analytics ingest.
/// </summary>
public sealed record TaskRunRecorded(
    Guid EventId,
    DateTime OccurredAt,
    Guid TaskRunId,
    Guid TaskDefinitionId,
    Guid BuildId,
    string TesterId,
    string Outcome,
    string? Note)
    : IDomainEvent;
