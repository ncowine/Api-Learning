using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Testers.Application.Abstractions;

namespace Testers.Application.Behaviors;

/// <summary>
/// Outermost behavior. Emits one structured log per request with timing, the request type, and the
/// current user — success or failure. Correlation IDs come from Serilog enrichers (HTTP middleware
/// in the Api project) and are attached automatically to every log line in the request scope.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        logger.LogInformation(
            "Dispatching {RequestName} for {UserId} ({UserKind})",
            requestName, currentUser.Id, currentUser.Kind);

        try
        {
            var response = await next();
            logger.LogInformation(
                "{RequestName} completed in {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "{RequestName} failed in {ElapsedMs}ms: {ExceptionType}",
                requestName, sw.ElapsedMilliseconds, ex.GetType().Name);
            throw;
        }
    }
}
