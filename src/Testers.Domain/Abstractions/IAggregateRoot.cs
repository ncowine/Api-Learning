namespace Testers.Domain.Abstractions;

/// <summary>
/// Non-generic facet of <see cref="AggregateRoot{TId}"/>. Lets the OutboxInterceptor
/// (Infrastructure) walk the change tracker for aggregates without knowing the concrete
/// <c>TId</c> of each one. Domain code should usually depend on <c>AggregateRoot&lt;TId&gt;</c>
/// directly; this interface is the seam Infrastructure uses for type-erased iteration.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
