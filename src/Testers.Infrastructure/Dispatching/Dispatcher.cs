using Microsoft.Extensions.DependencyInjection;
using Testers.Application.Abstractions;

namespace Testers.Infrastructure.Dispatching;

/// <summary>
/// Reflection-based <see cref="IDispatcher"/> implementation.
///
/// For each <see cref="Send{TResponse}"/> call: resolves the runtime
/// <c>IRequestHandler&lt;TRequest, TResponse&gt;</c> from DI, plus all registered
/// <c>IPipelineBehavior&lt;TRequest, TResponse&gt;</c>s for that pair. Wraps the handler in the
/// behaviors in registration order (first-registered = outermost) and invokes.
///
/// Reflection cost (one <c>MakeGenericType</c> + a few <c>Invoke</c> calls per request) is in the
/// microseconds — negligible next to typical DB round-trips. If profiling ever shows this as a hot
/// path, swap in compiled-expression caches keyed by <c>(TRequest, TResponse)</c>; the surface
/// stays identical.
/// </summary>
internal sealed class Dispatcher(IServiceProvider services) : IDispatcher
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = services.GetRequiredService(handlerType);
        var handlerHandle = handlerType.GetMethod("Handle")
            ?? throw new InvalidOperationException($"{handlerType.Name}.Handle missing.");

        // Innermost step: invoke the handler.
        RequestHandlerDelegate<TResponse> next = () =>
            (Task<TResponse>)handlerHandle.Invoke(handler, new object[] { request, ct })!;

        // Wrap each behavior around it. Reverse iteration so the FIRST-registered behavior
        // ends up OUTERMOST (matches the order in DependencyInjection.cs comments).
        var behaviors = services.GetServices(behaviorType).Reverse().ToArray();
        var behaviorHandle = behaviorType.GetMethod("Handle")
            ?? throw new InvalidOperationException($"{behaviorType.Name}.Handle missing.");

        foreach (var behavior in behaviors)
        {
            var current = next;
            var instance = behavior!;
            next = () => (Task<TResponse>)behaviorHandle.Invoke(instance, new object[] { request, current, ct })!;
        }

        return next();
    }
}
