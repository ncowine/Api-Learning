using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Testers.SharedKernel.Exceptions;
using ValidationException = Testers.SharedKernel.Exceptions.ValidationException;

namespace Testers.Api.Errors;

// Maps our Application exceptions to RFC 7807 ProblemDetails. Wired via
// AddExceptionHandler<ApiExceptionHandler>() + app.UseExceptionHandler().
internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, problem) = Map(exception, httpContext);

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            logger.LogWarning(exception, "Handled exception mapped to {Status}: {Message}", status, exception.Message);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }

    private static (int Status, ProblemDetails Problem) Map(Exception ex, HttpContext ctx) => ex switch
    {
        ValidationException ve => (
            StatusCodes.Status400BadRequest,
            new ValidationProblemDetails(ve.Errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://datatracker.ietf.org/doc/html/rfc7807",
                Instance = ctx.Request.Path,
            }),
        NotFoundException nfe => (
            StatusCodes.Status404NotFound,
            new ProblemDetails { Title = "Resource not found", Status = StatusCodes.Status404NotFound, Detail = nfe.Message, Instance = ctx.Request.Path }),
        ConflictException ce => (
            StatusCodes.Status409Conflict,
            new ProblemDetails { Title = "Conflict", Status = StatusCodes.Status409Conflict, Detail = ce.Message, Instance = ctx.Request.Path }),
        ForbiddenException fe => (
            StatusCodes.Status403Forbidden,
            new ProblemDetails { Title = "Forbidden", Status = StatusCodes.Status403Forbidden, Detail = fe.Message, Instance = ctx.Request.Path }),
        _ => (
            StatusCodes.Status500InternalServerError,
            new ProblemDetails
            {
                Title = "Internal server error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred. See the server logs for details.",  // don't leak details
                Instance = ctx.Request.Path,
            }),
    };
}
