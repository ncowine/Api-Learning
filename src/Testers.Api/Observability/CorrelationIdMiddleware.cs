using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Testers.Api.Observability;

/// <summary>
/// Ensures every request has a correlation id. Reads <c>X-Correlation-Id</c> from the request
/// (clients can supply one to stitch a multi-call workflow); generates a fresh GUID if absent.
/// Pushes it to Serilog's <see cref="LogContext"/> so every log line in the request scope is
/// enriched automatically — including DB queries and outbox message headers.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var supplied)
            && !string.IsNullOrWhiteSpace(supplied.ToString())
                ? supplied.ToString()
                : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;
        context.Items[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}
