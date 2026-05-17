using Microsoft.EntityFrameworkCore;

namespace Testers.Application.Abstractions;

// Handlers depend on this, not the concrete TestPlanDbContext in Infrastructure.
public interface ITestPlanDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
