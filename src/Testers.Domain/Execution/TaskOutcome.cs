using Testers.Domain.Abstractions;

namespace Testers.Domain.Execution;

/// <summary>
/// The result a tester records against a task. Persisted as <see cref="SmartEnum{TEnum}.Value"/>
/// (int) via an EF value converter, displayed/serialised as <see cref="SmartEnum{TEnum}.Name"/>.
///
/// <see cref="IsTerminal"/> distinguishes outcomes that close out a task on a build (PASS/FAIL)
/// from outcomes that leave it open for re-testing later (SKIP/BLOCK).
/// </summary>
public sealed class TaskOutcome : SmartEnum<TaskOutcome>
{
    public static readonly TaskOutcome Pass = new(1, "PASS", isTerminal: true);
    public static readonly TaskOutcome Fail = new(2, "FAIL", isTerminal: true);
    public static readonly TaskOutcome Skip = new(3, "SKIP", isTerminal: false);
    public static readonly TaskOutcome Block = new(4, "BLOCK", isTerminal: false);

    public bool IsTerminal { get; }

    private TaskOutcome(int value, string name, bool isTerminal) : base(value, name)
    {
        IsTerminal = isTerminal;
    }
}
