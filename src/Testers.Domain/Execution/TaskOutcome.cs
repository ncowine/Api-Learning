using Testers.Domain.Abstractions;

namespace Testers.Domain.Execution;

public sealed class TaskOutcome : SmartEnum<TaskOutcome>
{
    public static readonly TaskOutcome Pass = new(1, "PASS", isTerminal: true);
    public static readonly TaskOutcome Fail = new(2, "FAIL", isTerminal: true);
    public static readonly TaskOutcome Skip = new(3, "SKIP", isTerminal: false);
    public static readonly TaskOutcome Block = new(4, "BLOCK", isTerminal: false);

    // PASS/FAIL close the task on this build; SKIP/BLOCK leave it open for retry.
    public bool IsTerminal { get; }

    private TaskOutcome(int value, string name, bool isTerminal) : base(value, name) =>
        IsTerminal = isTerminal;
}
