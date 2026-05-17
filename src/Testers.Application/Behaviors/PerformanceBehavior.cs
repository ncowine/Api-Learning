using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Testers.Application.Abstractions;

namespace Testers.Application.Behaviors;

/// <summary>
/// Innermost behavior — measures only handler time, not validation or transaction setup.
/// Emits a Warning if any request exceeds <see cref="SlowThresholdMs"/>. Surfaces accidental
/// N+1 queries, missing indexes, and other latency regressions early. Tune the threshold per
/// environment via configuration if needed.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const int SlowThresholdMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next();
        }
        finally
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds > SlowThresholdMs)
            {
                logger.LogWarning(
                    "Slow request {RequestName}: {ElapsedMs}ms (threshold {ThresholdMs}ms)",
                    typeof(TRequest).Name, sw.ElapsedMilliseconds, SlowThresholdMs);
            }
        }
    }
}
