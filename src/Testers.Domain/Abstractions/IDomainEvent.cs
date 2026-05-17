namespace Testers.Domain.Abstractions;

public interface IDomainEvent
{
    Guid EventId { get; }   // used as the RabbitMQ message-id for consumer dedup
    DateTime OccurredAt { get; }
}
