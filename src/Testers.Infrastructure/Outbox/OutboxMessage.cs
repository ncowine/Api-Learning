namespace Testers.Infrastructure.Outbox;

/// <summary>
/// One row per domain event captured by the OutboxInterceptor. The <c>OutboxPublisher</c> background
/// service (sub-step b) polls unprocessed rows and publishes to RabbitMQ, marking
/// <see cref="ProcessedAt"/> once the broker confirms publish. Failed publishes increment
/// <see cref="AttemptCount"/> and record <see cref="LastError"/>; rows that exceed a retry cap go
/// to the DLQ.
///
/// Lives in the App DB. Always written in the same transaction as the business write that raised
/// the event, so we never lose an event under crash conditions (no torn writes).
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }

    /// <summary>FQ type name of the IDomainEvent, e.g. <c>"Testers.Domain.Execution.TaskRunRecorded"</c>.</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>RabbitMQ routing key, e.g. <c>"taskrun.recorded.v1"</c>. Built from event type metadata.</summary>
    public string RoutingKey { get; private set; } = string.Empty;

    /// <summary>The event serialised as JSON. Stored in a MySQL <c>JSON</c> column.</summary>
    public string Payload { get; private set; } = string.Empty;

    public DateTime OccurredAt { get; private set; }

    /// <summary>Set once the broker has confirmed the publish. Null while unprocessed.</summary>
    public DateTime? ProcessedAt { get; private set; }

    public int AttemptCount { get; private set; }

    public string? LastError { get; private set; }

    /// <summary>Correlation id from the originating request (HTTP correlation header).</summary>
    public string? CorrelationId { get; private set; }

    private OutboxMessage()
    {
        // EF Core materialisation only.
    }

    internal OutboxMessage(
        Guid id,
        string eventType,
        string routingKey,
        string payload,
        DateTime occurredAt,
        string? correlationId)
    {
        Id = id;
        EventType = eventType;
        RoutingKey = routingKey;
        Payload = payload;
        OccurredAt = occurredAt;
        CorrelationId = correlationId;
    }

    /// <summary>Called by the OutboxPublisher once RabbitMQ confirms the publish.</summary>
    internal void MarkProcessed(DateTime when)
    {
        ProcessedAt = when;
    }

    /// <summary>Called by the OutboxPublisher when a publish attempt fails (transient).</summary>
    internal void RecordFailure(string error)
    {
        AttemptCount++;
        LastError = error;
    }
}
