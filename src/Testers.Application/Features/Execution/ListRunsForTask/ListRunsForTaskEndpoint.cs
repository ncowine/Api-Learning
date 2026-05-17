using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Testers.Application.Abstractions;
using Testers.Contracts.Execution;

namespace Testers.Application.Features.Execution.ListRunsForTask;

public sealed class ListRunsForTaskEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(Routes.Tasks.RunsTemplate,
                async (Guid taskDefinitionId, IDispatcher dispatcher, CancellationToken ct) =>
                {
                    var result = await dispatcher.Send(new ListRunsForTaskQuery(taskDefinitionId), ct);
                    return Results.Ok(result);
                })
            .RequireAuthorization()
            .WithName("ListRunsForTask")
            .WithTags("Execution")
            .Produces<IReadOnlyList<RunSummary>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
