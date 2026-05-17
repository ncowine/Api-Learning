namespace Testers.Application.Abstractions;

/// <summary>
/// Handler for a single request type. Exactly one handler per request. Resolved from DI by the
/// <see cref="IDispatcher"/> implementation, wrapped in registered <see cref="IPipelineBehavior{TRequest,TResponse}"/>s.
/// </summary>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}
