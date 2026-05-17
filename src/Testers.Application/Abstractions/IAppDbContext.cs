using Microsoft.EntityFrameworkCore;

namespace Testers.Application.Abstractions;

// Handlers depend on this, not the concrete AppDbContext in Infrastructure.
public interface IAppDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
