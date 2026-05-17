using Testers.Application.Abstractions;
using Testers.Domain.Execution;

namespace Testers.Application.Features.Execution.ActionTaskRun;

internal sealed class ActionTaskRunHandler(
    ITestPlanDbContext testPlanDb,
    IAppDbContext appDb,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<ActionTaskRunCommand, ActionTaskRunResult>
{
    public async Task<ActionTaskRunResult> Handle(ActionTaskRunCommand request, CancellationToken ct)
    {
        var outcome = TaskOutcome.FromName(request.Outcome);
        var now = clock.UtcNow;

        var run = new TaskRun(
            request.TaskDefinitionId,
            request.BuildId,
            currentUser.Id,
            outcome,
            request.Note,
            now);

        // testPlanDb save first: OutboxInterceptor picks up the TaskRunRecorded event and
        // queues the outbox row into appDb. appDb save commits the row. UoW behavior wraps
        // both in one shared-connection transaction.
        testPlanDb.Set<TaskRun>().Add(run);
        await testPlanDb.SaveChangesAsync(ct);
        await appDb.SaveChangesAsync(ct);

        return new ActionTaskRunResult(run.Id, outcome.Name, now);
    }
}
