using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Testers.Application.Abstractions;
using Testers.Contracts.Execution;

namespace Testers.Application.Features.Execution.GetRunDetail;

public sealed class GetRunDetailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(Routes.Runs.DetailTemplate,
                async (Guid taskRunId, IDispatcher dispatcher, CancellationToken ct) =>
                {
                    var result = await dispatcher.Send(new GetRunDetailQuery(taskRunId), ct);
                    return Results.Ok(result);
                })
            .RequireAuthorization()
            .WithName("GetRunDetail")
            .WithTags("Execution")
            .Produces<RunDetail>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
