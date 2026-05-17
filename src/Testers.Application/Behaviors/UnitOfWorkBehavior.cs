using Testers.Application.Abstractions;

namespace Testers.Application.Behaviors;

/// <summary>
/// Opens a transaction for commands only (queries skip). Wraps the handler call; commits on
/// success, rolls back on exception. The actual transaction lifecycle (shared
/// <c>MySqlConnection</c> across both DbContexts so cross-DB writes are atomic) is implemented
/// by <see cref="IUnitOfWork"/> in Infrastructure.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork uow)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Queries (IQuery<T>) don't need a transaction; pass through.
        if (request is not ICommand<TResponse> && request is not ICommand)
        {
            return await next();
        }

        await uow.BeginAsync(ct);
        try
        {
            var response = await next();
            await uow.CommitAsync(ct);
            return response;
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
    }
}
