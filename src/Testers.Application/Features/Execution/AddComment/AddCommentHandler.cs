using Microsoft.EntityFrameworkCore;
using Testers.SharedKernel.Abstractions;
using Testers.SharedKernel.Exceptions;
using Testers.Contracts.Execution;
using Testers.Domain.Execution;

namespace Testers.Application.Features.Execution.AddComment;

// Cross-DB write. TaskRun (with new Comment) goes to TestPlan DB; OutboxInterceptor sees the
// CommentAddedToRun domain event and queues an OutboxMessage into the App DB. Both saves
// commit in one shared-connection transaction via UnitOfWorkBehavior.
internal sealed class AddCommentHandler(
    ITestPlanDbContext testPlanDb,
    IAppDbContext appDb,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<AddCommentCommand, AddCommentResult>
{
    public async Task<AddCommentResult> Handle(AddCommentCommand cmd, CancellationToken ct)
    {
        // Tracking-enabled load (we're going to mutate). Include the owned collection so EF
        // tracks the appended Comment.
        var run = await testPlanDb.Set<TaskRun>()
            .Include(r => r.Comments)
            .FirstOrDefaultAsync(r => r.Id == cmd.TaskRunId, ct);

        if (run is null)
            throw new NotFoundException(nameof(TaskRun), cmd.TaskRunId);

        var comment = run.AddComment(cmd.Body, currentUser.Id, clock.UtcNow);

        await testPlanDb.SaveChangesAsync(ct);
        await appDb.SaveChangesAsync(ct);

        return new AddCommentResult(comment.Id, comment.AddedAt);
    }
}
