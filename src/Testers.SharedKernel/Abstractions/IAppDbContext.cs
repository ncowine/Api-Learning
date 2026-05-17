using Microsoft.EntityFrameworkCore;

namespace Testers.SharedKernel.Abstractions;

// Marker interface for the user-owned DbContext. Handlers depend on this, not the concrete
// AppDbContext in Infrastructure. The "App" naming is conventional - in another app you'd
// name your own marker accordingly.
public interface IAppDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
