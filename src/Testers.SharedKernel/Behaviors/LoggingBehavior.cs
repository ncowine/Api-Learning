using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Testers.SharedKernel.Abstractions;

namespace Testers.SharedKernel.Behaviors;

// Outermost. One structured log per request. Correlation id comes from log enrichers
// pushed by the Api project's middleware - we don't need to touch it here.
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        logger.LogInformation("Dispatching {RequestName} for {UserId} ({UserKind})",
            name, currentUser.Id, currentUser.Kind);

        try
        {
            var response = await next();
            logger.LogInformation("{RequestName} completed in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{RequestName} failed in {ElapsedMs}ms: {ExceptionType}",
                name, sw.ElapsedMilliseconds, ex.GetType().Name);
            throw;
        }
    }
}
