namespace Testers.Domain.Abstractions;

/// <summary>
/// Marker for events raised by an aggregate during a business operation.
///
/// Captured by the OutboxInterceptor (Infrastructure) inside <c>SaveChangesAsync</c> and
/// serialised into the outbox table in the SAME transaction as the originating business write.
/// The OutboxPublisher background service then publishes them to RabbitMQ, using
/// <see cref="EventId"/> as the message id so consumers have an idempotency key.
///
/// Implementations should be immutable records carrying the minimum data downstream consumers
/// need — they outlive any in-memory aggregate state.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Stable identifier so the event can be deduplicated by consumers (inbox pattern).</summary>
    Guid EventId { get; }

    /// <summary>UTC wall-clock moment the event was raised by the aggregate.</summary>
    DateTime OccurredAt { get; }
}
