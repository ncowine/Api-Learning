using Microsoft.EntityFrameworkCore;

namespace Testers.SharedKernel.Abstractions;

// Marker interface for the shared TestPlan DbContext. Handlers depend on this, not the
// concrete TestPlanDbContext in Infrastructure.
public interface ITestPlanDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
