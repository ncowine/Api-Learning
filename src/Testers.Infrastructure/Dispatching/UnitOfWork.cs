using Microsoft.EntityFrameworkCore;
using Testers.Application.Abstractions;
using Testers.Infrastructure.Persistence;

namespace Testers.Infrastructure.Dispatching;

/// <summary>
/// Owns the shared transaction. Begins the transaction on the <see cref="SharedConnection"/>,
/// then tells both DbContexts to participate in it via <c>Database.UseTransactionAsync</c>.
/// Commit and rollback delegate straight back to <see cref="SharedConnection"/> so the lifecycle
/// stays in one place.
///
/// Handlers still call <c>SaveChangesAsync</c> on whichever DbContext they need; this just wraps
/// the whole sequence in one atomic transaction across both contexts on the shared MySQL connection.
/// </summary>
internal sealed class UnitOfWork(
    SharedConnection sharedConnection,
    AppDbContext appDb,
    TestPlanDbContext testPlanDb) : IUnitOfWork
{
    public async Task BeginAsync(CancellationToken ct = default)
    {
        await sharedConnection.BeginTransactionAsync(ct).ConfigureAwait(false);

        var tx = sharedConnection.CurrentTransaction
            ?? throw new InvalidOperationException("BeginTransactionAsync did not set a current transaction.");

        await appDb.Database.UseTransactionAsync(tx, ct).ConfigureAwait(false);
        await testPlanDb.Database.UseTransactionAsync(tx, ct).ConfigureAwait(false);
    }

    public Task CommitAsync(CancellationToken ct = default) => sharedConnection.CommitAsync(ct);

    public Task RollbackAsync(CancellationToken ct = default) => sharedConnection.RollbackAsync(ct);
}
