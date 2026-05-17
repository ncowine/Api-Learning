using Microsoft.EntityFrameworkCore;
using Testers.Application.Abstractions;
using Testers.Application.Exceptions;
using Testers.Contracts.Execution;
using Testers.Domain.Execution;

namespace Testers.Application.Features.Execution.LinkBug;

// Cross-DB write. BugLink goes to TestPlan DB; OutboxInterceptor picks up the BugLinkedToRun
// event and queues an OutboxMessage in App DB. Both saves commit atomically via UoW.
internal sealed class LinkBugHandler(
    ITestPlanDbContext testPlanDb,
    IAppDbContext appDb,
    IClock clock) : IRequestHandler<LinkBugCommand, LinkBugResult>
{
    public async Task<LinkBugResult> Handle(LinkBugCommand cmd, CancellationToken ct)
    {
        var run = await testPlanDb.Set<TaskRun>()
            .Include(r => r.BugLinks)
            .FirstOrDefaultAsync(r => r.Id == cmd.TaskRunId, ct);

        if (run is null)
            throw new NotFoundException(nameof(TaskRun), cmd.TaskRunId);

        var link = run.LinkBug(cmd.ExternalBugId, cmd.BugTrackerUrl, clock.UtcNow);

        await testPlanDb.SaveChangesAsync(ct);
        await appDb.SaveChangesAsync(ct);

        return new LinkBugResult(link.Id, link.LinkedAt);
    }
}
