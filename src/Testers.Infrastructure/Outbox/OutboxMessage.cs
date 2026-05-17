namespace Testers.Infrastructure.Outbox;

// Written in the same tx as the business write that raised the event. OutboxPublisher
// drains the table and pushes to RabbitMQ.
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string RoutingKey { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;  // JSON column
    public DateTime OccurredAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }            // null while unprocessed
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public string? CorrelationId { get; private set; }

    private OutboxMessage() { }  // EF

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

    internal void MarkProcessed(DateTime when) => ProcessedAt = when;

    internal void RecordFailure(string error)
    {
        AttemptCount++;
        LastError = error;
    }
}
