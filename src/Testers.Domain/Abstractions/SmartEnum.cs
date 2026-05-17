using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Testers.Domain.Abstractions;

/// <summary>
/// Type-safe enum-with-behavior pattern. Each "enum option" is a static readonly field on the
/// concrete subclass, carrying any associated data and methods. Example:
///
/// <code>
/// public sealed class TaskOutcome : SmartEnum&lt;TaskOutcome&gt;
/// {
///     public static readonly TaskOutcome Pass = new(1, "PASS", isTerminal: true);
///     public static readonly TaskOutcome Fail = new(2, "FAIL", isTerminal: true);
///
///     public bool IsTerminal { get; }
///
///     private TaskOutcome(int value, string name, bool isTerminal) : base(value, name)
///         =&gt; IsTerminal = isTerminal;
/// }
/// </code>
///
/// EF Core persists the <see cref="Value"/> (int) via a value converter — no string drift, no
/// switch statements scattered across handlers. Reflection-based discovery happens once on first
/// access (Lazy), then everything is O(1) dictionary lookups.
/// </summary>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
    Justification = "Curiously Recurring Template Pattern requires per-type static factories (All/FromValue/FromName).")]
public abstract class SmartEnum<TEnum> : IEquatable<SmartEnum<TEnum>>
    where TEnum : SmartEnum<TEnum>
{
    private static readonly Lazy<IReadOnlyDictionary<int, TEnum>> _byValue =
        new(() => DiscoverAll().ToDictionary(e => e.Value));

    private static readonly Lazy<IReadOnlyDictionary<string, TEnum>> _byName =
        new(() => DiscoverAll().ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase));

    public int Value { get; }

    public string Name { get; }

    protected SmartEnum(int value, string name)
    {
        Value = value;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public static IReadOnlyCollection<TEnum> All => (IReadOnlyCollection<TEnum>)_byValue.Value.Values;

    public static TEnum FromValue(int value) =>
        _byValue.Value.TryGetValue(value, out var found)
            ? found
            : throw new ArgumentOutOfRangeException(
                nameof(value), value, $"No {typeof(TEnum).Name} option with value {value}.");

    public static TEnum FromName(string name) =>
        _byName.Value.TryGetValue(name, out var found)
            ? found
            : throw new ArgumentOutOfRangeException(
                nameof(name), name, $"No {typeof(TEnum).Name} option with name '{name}'.");

    public bool Equals(SmartEnum<TEnum>? other) =>
        other is not null && GetType() == other.GetType() && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as SmartEnum<TEnum>);

    public override int GetHashCode() => HashCode.Combine(GetType(), Value);

    public override string ToString() => Name;

    public static bool operator ==(SmartEnum<TEnum>? a, SmartEnum<TEnum>? b) =>
        ReferenceEquals(a, b) || (a is not null && a.Equals(b));

    public static bool operator !=(SmartEnum<TEnum>? a, SmartEnum<TEnum>? b) => !(a == b);

    private static IEnumerable<TEnum> DiscoverAll() =>
        typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => typeof(TEnum).IsAssignableFrom(f.FieldType))
            .Select(f => (TEnum)f.GetValue(null)!);
}
