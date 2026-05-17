namespace Testers.Application.Abstractions;

/// <summary>Continuation delegate passed to behaviors; calling it invokes the next behavior, ultimately reaching the handler.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Cross-cutting concern that wraps every request: logging, validation, transaction, performance, etc.
/// Behaviors compose in DI registration order — outermost first. Call <c>next()</c> to continue the
/// pipeline; throw to short-circuit. Multiple registrations of the open generic
/// <c>IPipelineBehavior&lt;,&gt;</c> are resolved as <c>IEnumerable&lt;...&gt;</c> by the dispatcher.
/// </summary>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}
