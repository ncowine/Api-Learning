using Microsoft.EntityFrameworkCore;
using Testers.SharedKernel.Abstractions;
using Testers.SharedKernel.Exceptions;
using Testers.Contracts.Execution;
using Testers.Domain.Execution;

namespace Testers.Application.Features.Execution.GetRunDetail;

internal sealed class GetRunDetailHandler(ITestPlanDbContext testPlanDb)
    : IRequestHandler<GetRunDetailQuery, RunDetail>
{
    public async Task<RunDetail> Handle(GetRunDetailQuery query, CancellationToken ct)
    {
        // Single query with two Includes - the cartesian JOIN of Comments x BugLinks is fine
        // here because both collections are typically small per run. For hot read paths where
        // collections get large, switch to .AsSplitQuery() (three separate SELECTs).
        var run = await testPlanDb.Set<TaskRun>()
            .AsNoTracking()
            .Include(r => r.Comments)
            .Include(r => r.BugLinks)
            .FirstOrDefaultAsync(r => r.Id == query.TaskRunId, ct);

        if (run is null)
            throw new NotFoundException(nameof(TaskRun), query.TaskRunId);

        return new RunDetail(
            run.Id, run.TaskDefinitionId, run.BuildId, run.TesterId,
            run.Outcome.Name, run.Note, run.ActionedAt,
            run.Comments.Select(c => new CommentDto(c.Id, c.Body, c.AuthorId, c.AddedAt)).ToList(),
            run.BugLinks.Select(b => new BugLinkDto(b.Id, b.ExternalBugId, b.BugTrackerUrl, b.LinkedAt)).ToList());
    }
}
