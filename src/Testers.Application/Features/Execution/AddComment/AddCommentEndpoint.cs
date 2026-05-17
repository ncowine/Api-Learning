using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Testers.Application.Abstractions;
using Testers.Contracts.Execution;

namespace Testers.Application.Features.Execution.AddComment;

public sealed class AddCommentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(Routes.Runs.CommentsTemplate,
                async (Guid taskRunId,
                       AddCommentRequest body,
                       IDispatcher dispatcher,
                       CancellationToken ct) =>
                {
                    var cmd = new AddCommentCommand(taskRunId, body.Body);
                    var result = await dispatcher.Send(cmd, ct);
                    return Results.Created(Routes.Runs.Detail(taskRunId), result);
                })
            .RequireAuthorization()
            .WithName("AddComment")
            .WithTags("Execution")
            .Produces<AddCommentResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
