namespace Testers.Domain.Abstractions;

/// <summary>
/// Base for all entities. An entity is defined by its identity (<see cref="Id"/>), not by its
/// attribute values — two TaskRuns with identical fields but different Ids are different entities.
///
/// The protected parameterless constructor exists so EF Core can materialise instances via
/// reflection without invoking the domain constructor.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IEquatable<TId>
{
    public TId Id { get; protected set; }

    protected Entity(TId id) => Id = id;

    protected Entity()
    {
        // EF Core materialisation only.
    }

    public bool Equals(Entity<TId>? other) =>
        other is not null
        && GetType() == other.GetType()
        && Id.Equals(other.Id);

    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b) =>
        ReferenceEquals(a, b) || (a is not null && a.Equals(b));

    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !(a == b);
}
