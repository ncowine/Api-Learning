using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Testers.SharedKernel.Abstractions;
using Testers.Contracts.Execution;

namespace Testers.Application.Features.Execution.ActionTaskRun;

public sealed class ActionTaskRunEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Routes.Tasks.RunsTemplate,
                async (Guid taskDefinitionId,
                       ActionTaskRunRequest body,
                       IDispatcher dispatcher,
                       CancellationToken ct) =>
                {
                    var cmd = new ActionTaskRunCommand(
                        taskDefinitionId, body.BuildId, body.Outcome, body.Note);
                    var result = await dispatcher.Send(cmd, ct);
                    return Results.Created(Routes.Runs.Detail(result.TaskRunId), result);
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
