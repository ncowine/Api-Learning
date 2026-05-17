namespace Testers.Application.Features.Execution.ActionTaskRun;

// POST body. Route binds taskDefinitionId.
public sealed record ActionTaskRunRequest(
    Guid BuildId,
    string Outcome,
    string? Note);
