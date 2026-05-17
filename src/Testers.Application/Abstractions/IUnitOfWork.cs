namespace Testers.Application.Abstractions;

// UoW behavior wraps commands in a shared-connection transaction so cross-DB writes
// (TaskRun in TestPlan DB + outbox row in App DB) commit atomically. Queries skip.
public interface IUnitOfWork
{
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
