using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Testers.Application.Exceptions;
using ValidationException = Testers.Application.Exceptions.ValidationException;

namespace Testers.Api.Errors;

/// <summary>
/// Maps Application-layer exceptions to RFC 7807 ProblemDetails responses with the right
/// HTTP status. Implements ASP.NET Core 8's <see cref="IExceptionHandler"/>; registered with
/// <c>AddExceptionHandler&lt;ApiExceptionHandler&gt;()</c> + <c>app.UseExceptionHandler()</c>.
/// </summary>
internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, problem) = Map(exception, httpContext);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            logger.LogWarning(exception,
                "Handled exception mapped to {Status}: {Message}", status, exception.Message);
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static (int Status, ProblemDetails Problem) Map(Exception ex, HttpContext ctx)
    {
        return ex switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                new ValidationProblemDetails(ve.Errors.ToDictionary(e => e.Key, e => e.Value))
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://datatracker.ietf.org/doc/html/rfc7807",
                    Instance = ctx.Request.Path,
                }
            ),
            NotFoundException nfe => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Title = "Resource not found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = nfe.Message,
                    Instance = ctx.Request.Path,
                }
            ),
            ConflictException ce => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Title = "Conflict",
                    Status = StatusCodes.Status409Conflict,
                    Detail = ce.Message,
                    Instance = ctx.Request.Path,
                }
            ),
            ForbiddenException fe => (
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = fe.Message,
                    Instance = ctx.Request.Path,
                }
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Status = StatusCodes.Status500InternalServerError,
                    // Don't leak exception details in the 500 case.
                    Detail = "An unexpected error occurred. See the server logs for details.",
                    Instance = ctx.Request.Path,
                }
            ),
        };
    }
}
