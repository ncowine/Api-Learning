namespace Testers.Application.Abstractions;

/// <summary>
/// A read-only request. The UnitOfWorkBehavior skips opening a transaction for queries.
/// Handlers should use <c>AsNoTracking()</c> on DbContext and project directly to DTOs.
/// </summary>
public interface IQuery<TResponse> : IRequest<TResponse>
{
}
