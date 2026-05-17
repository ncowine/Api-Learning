using Microsoft.EntityFrameworkCore;

namespace Testers.Application.Abstractions;

/// <summary>
/// Abstracts the shared TestPlan DbContext for feature handlers. Infrastructure registers
/// the concrete <c>TestPlanDbContext</c> as an implementation of this. Handlers call
/// <c>Set&lt;TaskRun&gt;()</c> (or other TestPlan-mapped entities) without referencing
/// Infrastructure types directly.
/// </summary>
public interface ITestPlanDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
