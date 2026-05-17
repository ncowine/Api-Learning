using Microsoft.EntityFrameworkCore;
using Testers.SharedKernel.Abstractions;
using Testers.Infrastructure.Persistence;

namespace Testers.Infrastructure.Dispatching;

// Begins the tx on SharedConnection, then UseTransactionAsync on both DbContexts so EF
// participates. Commit/Rollback delegate back to SharedConnection.
internal sealed class UnitOfWork(
    SharedConnection sharedConnection,
    AppDbContext appDb,
    TestPlanDbContext testPlanDb) : IUnitOfWork
{
    public async Task BeginAsync(CancellationToken ct = default)
    {
        await sharedConnection.BeginTransactionAsync(ct);

        var tx = sharedConnection.CurrentTransaction
            ?? throw new InvalidOperationException("BeginTransactionAsync did not set a current transaction.");

        await appDb.Database.UseTransactionAsync(tx, ct);
        await testPlanDb.Database.UseTransactionAsync(tx, ct);
    }

    public Task CommitAsync(CancellationToken ct = default) => sharedConnection.CommitAsync(ct);
    public Task RollbackAsync(CancellationToken ct = default) => sharedConnection.RollbackAsync(ct);
}
