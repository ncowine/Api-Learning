using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Testers.Application.Abstractions;

namespace Testers.Application.Features.Execution.ActionTaskRun;

public sealed class ActionTaskRunEndpoint : IEndpoint
{
    // HTTP body shape. Nested so we don't pay for a separate file for ~3 properties.
    public sealed record Body(Guid BuildId, string Outcome, string? Note);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/tasks/{taskDefinitionId:guid}/runs",
                async (Guid taskDefinitionId,
                       Body body,
                       IDispatcher dispatcher,
                       CancellationToken ct) =>
                {
                    var cmd = new ActionTaskRunCommand(
                        taskDefinitionId, body.BuildId, body.Outcome, body.Note);
                    var result = await dispatcher.Send(cmd, ct);
                    return Results.Created($"/api/v1/tasks/{taskDefinitionId}/runs/{result.TaskRunId}", result);
                })
            .RequireAuthorization()
            .WithName("ActionTaskRun")
            .WithTags("Execution")
            .Produces<ActionTaskRunResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
