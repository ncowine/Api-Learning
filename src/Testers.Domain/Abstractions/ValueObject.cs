namespace Testers.Domain.Abstractions;

// Equality by components, not by id.
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other) =>
        other is not null
        && GetType() == other.GetType()
        && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents()) hash.Add(component);
        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? a, ValueObject? b) =>
        ReferenceEquals(a, b) || (a is not null && a.Equals(b));
    public static bool operator !=(ValueObject? a, ValueObject? b) => !(a == b);
}
