using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Testers.Api.Observability;

// Reads X-Correlation-Id from the request (clients can supply) or generates one. Pushes into
// Serilog's LogContext so every log line in the scope is enriched.
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var supplied)
            && !string.IsNullOrWhiteSpace(supplied.ToString())
                ? supplied.ToString()
                : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;
        context.Items[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await next(context);
    }
}
