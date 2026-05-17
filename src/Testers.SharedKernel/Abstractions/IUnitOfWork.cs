namespace Testers.SharedKernel.Abstractions;

// UoW behavior wraps commands in a shared-connection transaction so cross-DB writes commit
// atomically. Queries skip.
public interface IUnitOfWork
{
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
