using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Testers.SharedKernel.Abstractions;
using Testers.Contracts.Execution;

namespace Testers.Application.Features.Execution.ListRunsForBuild;

public sealed class ListRunsForBuildEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(Routes.Builds.RunsTemplate,
                async (Guid buildId, IDispatcher dispatcher, CancellationToken ct) =>
                {
                    var result = await dispatcher.Send(new ListRunsForBuildQuery(buildId), ct);
                    return Results.Ok(result);
                })
            .RequireAuthorization()
            .WithName("ListRunsForBuild")
            .WithTags("Execution")
            .Produces<IReadOnlyList<RunSummary>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
