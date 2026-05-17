namespace Testers.Infrastructure.Outbox;

/// <summary>
/// One row per (MessageId, ConsumerName) pair successfully processed by a RabbitMQ consumer.
/// RabbitMQ delivery is at-least-once, so consumers ALWAYS see duplicates eventually. Each
/// consumer checks this table at the start of message handling and ACKs without re-processing
/// if a row already exists. The row is inserted inside the same transaction as the consumer's
/// business write, giving exactly-once *effect* on top of at-least-once delivery.
///
/// Old rows can be pruned after N days (longer than the broker's max possible redelivery delay)
/// to keep the table small.
/// </summary>
public sealed class InboxMessage
{
    /// <summary>The RabbitMQ message id (the originating OutboxMessage.Id from the publishing service).</summary>
    public Guid MessageId { get; private set; }

    /// <summary>The consumer name — same MessageId can be processed by multiple distinct consumers.</summary>
    public string ConsumerName { get; private set; } = string.Empty;

    public DateTime ProcessedAt { get; private set; }

    private InboxMessage()
    {
        // EF Core materialisation only.
    }

    internal InboxMessage(Guid messageId, string consumerName, DateTime processedAt)
    {
        MessageId = messageId;
        ConsumerName = consumerName;
        ProcessedAt = processedAt;
    }
}
