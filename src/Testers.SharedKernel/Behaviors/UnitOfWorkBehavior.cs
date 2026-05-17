using Testers.SharedKernel.Abstractions;

namespace Testers.SharedKernel.Behaviors;

// Commands only - queries pass through. Begins shared-connection tx, commits on success.
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork uow)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ICommand<TResponse> && request is not ICommand) return await next();

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
