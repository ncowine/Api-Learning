using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Testers.Application.Abstractions;

namespace Testers.Application.Features.Execution.ActionTaskRun;

/// <summary>
/// HTTP endpoint for the ActionTaskRun feature. Auto-discovered by the Api project's
/// EndpointScanner; mapped under <c>/api/v1</c> automatically. Adding a new slice does not
/// require touching <c>Program.cs</c>.
/// </summary>
public sealed class ActionTaskRunEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/tasks/{taskDefinitionId:guid}/runs",
                async (
                    Guid taskDefinitionId,
                    ActionTaskRunRequest body,
                    IDispatcher dispatcher,
                    CancellationToken ct) =>
                {
                    var cmd = new ActionTaskRunCommand(
                        taskDefinitionId,
                        body.BuildId,
                        body.Outcome,
                        body.Note);
                    var result = await dispatcher.Send(cmd, ct).ConfigureAwait(false);
                    return Results.Created($"/api/v1/tasks/{taskDefinitionId}/runs/{result.TaskRunId}", result);
                })
            .RequireAuthorization()
            .WithName("ActionTaskRun")
            .WithSummary("Record a tester's action on a task for a specific build")
            .WithTags("Execution")
            .Produces<ActionTaskRunResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
