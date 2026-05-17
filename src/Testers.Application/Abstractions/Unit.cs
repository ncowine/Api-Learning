namespace Testers.Application.Abstractions;

/// <summary>
/// Represents "no meaningful return value" — equivalent to <c>void</c> but Task-friendly for
/// generic handler signatures. Use <c>Task&lt;Unit&gt;</c> instead of <c>Task</c> so
/// <c>IRequest&lt;Unit&gt;</c> works uniformly through the dispatcher and behaviors.
/// </summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;

    public static Task<Unit> Task => System.Threading.Tasks.Task.FromResult(Value);
}
