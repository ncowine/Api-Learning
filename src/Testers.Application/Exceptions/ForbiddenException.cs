namespace Testers.Application.Exceptions;

/// <summary>
/// Thrown when the caller is authenticated but not authorised for this specific action (e.g.
/// trying to delete another tester's comment, or service-only endpoint hit by a human user).
/// Mapped to <c>403 ProblemDetails</c> by the Api project's ExceptionHandler.
/// </summary>
public sealed class ForbiddenException(string message) : Exception(message)
{
}
