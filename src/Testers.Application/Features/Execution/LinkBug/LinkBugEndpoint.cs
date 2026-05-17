using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Testers.Application.Abstractions;
using Testers.Contracts.Execution;

namespace Testers.Application.Features.Execution.LinkBug;

public sealed class LinkBugEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Routes.Runs.BugLinksTemplate,
                async (Guid taskRunId,
                       LinkBugRequest body,
                       IDispatcher dispatcher,
                       CancellationToken ct) =>
                {
                    var cmd = new LinkBugCommand(taskRunId, body.ExternalBugId, body.BugTrackerUrl);
                    var result = await dispatcher.Send(cmd, ct);
                    return Results.Created(Routes.Runs.Detail(taskRunId), result);
                })
            .RequireAuthorization()
            .WithName("LinkBug")
            .WithTags("Execution")
            .Produces<LinkBugResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
