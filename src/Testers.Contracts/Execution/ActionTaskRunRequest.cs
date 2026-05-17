namespace Testers.Contracts.Execution;

// POST body for Routes.Tasks.Runs(taskDefinitionId). Route supplies taskDefinitionId.
public sealed record ActionTaskRunRequest(Guid BuildId, string Outcome, string? Note);
