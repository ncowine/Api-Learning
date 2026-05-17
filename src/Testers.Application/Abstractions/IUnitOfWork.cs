namespace Testers.Application.Abstractions;

/// <summary>
/// Transaction boundary for a command. The <c>UnitOfWorkBehavior</c> calls <see cref="BeginAsync"/>
/// before the handler runs and <see cref="CommitAsync"/> if the handler returns successfully
/// (or <see cref="RollbackAsync"/> on exception).
///
/// Handlers call <c>SaveChangesAsync</c> on whichever DbContext they need; the transaction
/// wraps them all so cross-DB writes (a TaskRun in the TestPlan DB plus an outbox row in the
/// App DB) commit atomically. The infrastructure implementation manages the shared
/// <c>MySqlConnection</c> that lets a single transaction span both DbContexts.
///
/// Queries don't open a transaction — the behavior short-circuits.
/// </summary>
public interface IUnitOfWork
{
    Task BeginAsync(CancellationToken ct = default);

    Task CommitAsync(CancellationToken ct = default);

    Task RollbackAsync(CancellationToken ct = default);
}
