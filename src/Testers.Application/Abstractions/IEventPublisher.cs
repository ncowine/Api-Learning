using Testers.Domain.Abstractions;

namespace Testers.Application.Abstractions;

/// <summary>
/// Publishes a domain event to the messaging infrastructure (RabbitMQ via the outbox).
///
/// Application code should NOT call this directly — it raises events on aggregates, the
/// OutboxInterceptor captures them at <c>SaveChangesAsync</c> time, and the OutboxPublisher
/// background service eventually calls <see cref="PublishAsync"/>. The interface lives in
/// Application so the OutboxPublisher (in Infrastructure) doesn't have to reference Application
/// just for a publish call.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent @event, CancellationToken ct = default);
}
