namespace Testers.Contracts.Execution;

public sealed record ActionTaskRunResult(Guid TaskRunId, string Outcome, DateTime ActionedAt);
