namespace Testers.Contracts.Execution;

// POST Routes.Tasks.Runs(taskDefinitionId)  - route supplies taskDefinitionId.

public sealed record ActionTaskRunRequest(Guid BuildId, string Outcome, string? Note);

public sealed record ActionTaskRunResult(Guid TaskRunId, string Outcome, DateTime ActionedAt);
