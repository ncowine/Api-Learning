using Microsoft.EntityFrameworkCore;
using Testers.Application.Abstractions;
using Testers.Domain.Execution;

namespace Testers.Application.Features.Execution.ActionTaskRun;

/// <summary>
/// Handler for <see cref="ActionTaskRunCommand"/>. Exercises the cross-DB write path end-to-end:
///
/// <list type="number">
///   <item>Construct the <see cref="TaskRun"/> aggregate (raises <c>TaskRunRecorded</c> internally).</item>
///   <item>Add to the TestPlan DB; call <c>SaveChangesAsync</c>. The OutboxInterceptor walks
///         both contexts' change trackers, finds the new TaskRun's event, serialises it as an
///         <c>OutboxMessage</c>, and queues that row into the App DB's change tracker.</item>
///   <item>Call <c>SaveChangesAsync</c> on the App DB to commit the queued outbox row.</item>
///   <item>The <c>UnitOfWorkBehavior</c> (pipeline behavior wrapping this handler) commits the
///         shared transaction covering both saves. The OutboxPublisher BackgroundService picks
///         up the row on its next poll and publishes to RabbitMQ.</item>
/// </list>
///
/// The handler depends on <see cref="ITestPlanDbContext"/> / <see cref="IAppDbContext"/> (in
/// Application/Abstractions) rather than the concrete EF types in Infrastructure — keeps unit
/// testing trivial (fakes implementing the interfaces) and the dependency direction clean.
/// </summary>
internal sealed class ActionTaskRunHandler(
    ITestPlanDbContext testPlanDb,
    IAppDbContext appDb,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<ActionTaskRunCommand, ActionTaskRunResult>
{
    public async Task<ActionTaskRunResult> Handle(ActionTaskRunCommand request, CancellationToken ct)
    {
        var outcome = TaskOutcome.FromName(request.Outcome);
        var now = clock.UtcNow;

        var run = new TaskRun(
            taskDefinitionId: request.TaskDefinitionId,
            buildId: request.BuildId,
            testerId: currentUser.Id,
            outcome: outcome,
            note: request.Note,
            actionedAt: now);

        // Step 1: business write -> TestPlan DB. OutboxInterceptor fires and queues
        // the TaskRunRecorded event into AppDb's change tracker as an OutboxMessage.
        testPlanDb.Set<TaskRun>().Add(run);
        await testPlanDb.SaveChangesAsync(ct).ConfigureAwait(false);

        // Step 2: commit the queued outbox row -> App DB. Both saves are atomic because the
        // UnitOfWorkBehavior wraps the entire handler call in one shared-connection transaction.
        await appDb.SaveChangesAsync(ct).ConfigureAwait(false);

        return new ActionTaskRunResult(run.Id, outcome.Name, run.ActionedAt);
    }
}
