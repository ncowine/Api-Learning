using Microsoft.EntityFrameworkCore;
using Testers.SharedKernel.Abstractions;
using Testers.Contracts.Execution;
using Testers.Domain.Execution;

namespace Testers.Application.Features.Execution.ListRunsForBuild;

internal sealed class ListRunsForBuildHandler(ITestPlanDbContext testPlanDb)
    : IRequestHandler<ListRunsForBuildQuery, IReadOnlyList<RunSummary>>
{
    public async Task<IReadOnlyList<RunSummary>> Handle(ListRunsForBuildQuery query, CancellationToken ct)
    {
        // Hits the (build_id, task_definition_id) index defined in TestPlanDbContext.
        var rows = await testPlanDb.Set<TaskRun>()
            .AsNoTracking()
            .Where(r => r.BuildId == query.BuildId)
            .OrderByDescending(r => r.ActionedAt)
            .Select(r => new { r.Id, r.TaskDefinitionId, r.BuildId, r.TesterId, r.Outcome, r.ActionedAt })
            .ToListAsync(ct);

        return rows
            .Select(r => new RunSummary(r.Id, r.TaskDefinitionId, r.BuildId, r.TesterId, r.Outcome.Name, r.ActionedAt))
            .ToList();
    }
}
