namespace Testers.Application.Features.Execution.ActionTaskRun;

/// <summary>Response body for a successful <c>ActionTaskRun</c>.</summary>
public sealed record ActionTaskRunResult(
    Guid TaskRunId,
    string Outcome,
    DateTime ActionedAt);
