using Microsoft.EntityFrameworkCore;

namespace Testers.Application.Abstractions;

/// <summary>
/// Abstracts the user-owned DbContext for feature handlers. Infrastructure registers the
/// concrete <c>AppDbContext</c> as an implementation of this so handlers in Application can
/// access DbSets without referencing Infrastructure types directly.
///
/// Handlers should only call <c>Set&lt;TEntity&gt;()</c> for entities mapped in the App DB
/// model (outbox, inbox, audit log, app-specific aggregates).
/// </summary>
public interface IAppDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
