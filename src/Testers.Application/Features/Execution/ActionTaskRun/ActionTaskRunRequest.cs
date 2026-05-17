namespace Testers.Application.Features.Execution.ActionTaskRun;

/// <summary>
/// HTTP request body for <c>POST /api/v1/tasks/{taskDefinitionId}/runs</c>.
/// The route binds <c>taskDefinitionId</c>; the body supplies the rest.
/// </summary>
public sealed record ActionTaskRunRequest(
    Guid BuildId,
    string Outcome,
    string? Note);
