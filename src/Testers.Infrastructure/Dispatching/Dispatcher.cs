using Microsoft.Extensions.DependencyInjection;
using Testers.Application.Abstractions;

namespace Testers.Infrastructure.Dispatching;

// Reflection-based dispatch. Per request: resolve handler + behaviors, compose behaviors outermost
// first, invoke. Reflection cost is microseconds vs ms of DB I/O - not worth caching yet.
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

        RequestHandlerDelegate<TResponse> next = () =>
            (Task<TResponse>)handlerHandle.Invoke(handler, new object[] { request, ct })!;

        // Reverse so first-registered ends up outermost.
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
