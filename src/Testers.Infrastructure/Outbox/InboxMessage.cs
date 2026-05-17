namespace Testers.Infrastructure.Outbox;

// Idempotency for incoming RabbitMQ messages. Consumer checks before processing, inserts
// in the same tx as its business write. Same MessageId can be processed by multiple consumers.
public sealed class InboxMessage
{
    public Guid MessageId { get; private set; }
    public string ConsumerName { get; private set; } = string.Empty;
    public DateTime ProcessedAt { get; private set; }

    private InboxMessage() { }  // EF

    internal InboxMessage(Guid messageId, string consumerName, DateTime processedAt)
    {
        MessageId = messageId;
        ConsumerName = consumerName;
        ProcessedAt = processedAt;
    }
}
