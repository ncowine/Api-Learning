using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Testers.SharedKernel.Abstractions;

namespace Testers.SharedKernel.Behaviors;

// Innermost - times only handler work, not validation / tx setup. Warns if slow.
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const int SlowMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try { return await next(); }
        finally
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds > SlowMs)
                logger.LogWarning("Slow {RequestName}: {ElapsedMs}ms (threshold {ThresholdMs}ms)",
                    typeof(TRequest).Name, sw.ElapsedMilliseconds, SlowMs);
        }
    }
}
