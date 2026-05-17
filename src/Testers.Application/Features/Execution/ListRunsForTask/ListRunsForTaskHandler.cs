using Microsoft.EntityFrameworkCore;
using Testers.SharedKernel.Abstractions;
using Testers.Contracts.Execution;
using Testers.Domain.Execution;

namespace Testers.Application.Features.Execution.ListRunsForTask;

internal sealed class ListRunsForTaskHandler(ITestPlanDbContext testPlanDb)
    : IRequestHandler<ListRunsForTaskQuery, IReadOnlyList<RunSummary>>
{
    public async Task<IReadOnlyList<RunSummary>> Handle(ListRunsForTaskQuery query, CancellationToken ct)
    {
        // Anonymous projection so EF only SELECTs the columns we need; .Outcome materialises
        // via the value converter (int -> TaskOutcome), then we read .Name in memory.
        var rows = await testPlanDb.Set<TaskRun>()
            .AsNoTracking()
            .Where(r => r.TaskDefinitionId == query.TaskDefinitionId)
            .OrderByDescending(r => r.ActionedAt)
            .Select(r => new { r.Id, r.TaskDefinitionId, r.BuildId, r.TesterId, r.Outcome, r.ActionedAt })
            .ToListAsync(ct);

        return rows
            .Select(r => new RunSummary(r.Id, r.TaskDefinitionId, r.BuildId, r.TesterId, r.Outcome.Name, r.ActionedAt))
            .ToList();
    }
}
