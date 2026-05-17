namespace Testers.Domain.Abstractions;

/// <summary>
/// Root of an aggregate — the consistency boundary. Only aggregate roots may be loaded and
/// persisted independently; entities and value objects inside live and die with their root.
///
/// Implements <see cref="IAuditable"/>: every aggregate root in this codebase gets Created/Modified
/// audit stamps populated by the AuditInterceptor at <c>SaveChangesAsync</c> time. Non-root entities
/// (e.g. owned types like <c>Comment</c> inside a <c>TaskRun</c>) do NOT get their own audit stamps;
/// the parent aggregate's stamps cover the whole graph.
///
/// Domain events raised via <see cref="Raise"/> are captured by the OutboxInterceptor during the
/// SAME <c>SaveChangesAsync</c> call and written to the outbox table atomically with the business
/// changes — see <see cref="IDomainEvent"/>.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAuditable, IAggregateRoot
    where TId : struct, IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // IAuditable. Private setters; EF Core's property access writes via reflection, domain code cannot.
    public DateTime CreatedAt { get; private set; }

    public string CreatedBy { get; private set; } = string.Empty;

    public DateTime? ModifiedAt { get; private set; }

    public string? ModifiedBy { get; private set; }

    protected AggregateRoot(TId id) : base(id) { }

    protected AggregateRoot()
    {
        // EF Core materialisation only.
    }

    /// <summary>Queue a domain event. Picked up by the OutboxInterceptor on the next <c>SaveChangesAsync</c>.</summary>
    protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);

    /// <summary>Called by the OutboxInterceptor after events have been written to the outbox.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
