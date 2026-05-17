namespace Testers.Domain.Abstractions;

// Domain events + audit fields. Both consumed by interceptors during SaveChanges.
public abstract class AggregateRoot<TId> : Entity<TId>, IAuditable, IAggregateRoot
    where TId : struct, IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    protected AggregateRoot(TId id) : base(id) { }
    protected AggregateRoot() { }  // EF

    protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
