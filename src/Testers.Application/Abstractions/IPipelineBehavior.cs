namespace Testers.Application.Abstractions;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

// Multiple open-generic registrations get composed by the dispatcher in DI registration order
// (first registered = outermost). Call next() to continue, throw to short-circuit.
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}
