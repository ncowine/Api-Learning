namespace Testers.Domain.Abstractions;

// Non-generic facet so OutboxInterceptor can walk aggregates without knowing TId.
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
