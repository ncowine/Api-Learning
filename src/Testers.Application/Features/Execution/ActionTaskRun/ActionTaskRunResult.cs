namespace Testers.Application.Features.Execution.ActionTaskRun;

public sealed record ActionTaskRunResult(
    Guid TaskRunId,
    string Outcome,
    DateTime ActionedAt);
