namespace Testers.Domain.Abstractions;

/// <summary>
/// Base for value objects: identity is defined by the equality of all components, not by an Id.
/// Two <c>Money</c> instances with the same currency and amount are interchangeable.
///
/// Implementations expose the components via <see cref="GetEqualityComponents"/>. Order matters —
/// keep it stable so the hash code stays stable across refactors.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>The components that determine equality. Order matters for hashing.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other) =>
        other is not null
        && GetType() == other.GetType()
        && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? a, ValueObject? b) =>
        ReferenceEquals(a, b) || (a is not null && a.Equals(b));

    public static bool operator !=(ValueObject? a, ValueObject? b) => !(a == b);
}
