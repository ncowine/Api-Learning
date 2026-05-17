namespace Testers.Application.Exceptions;

/// <summary>
/// Thrown when a write would violate a domain invariant or business rule (e.g. cannot amend a
/// TaskRun that's already been signed off, duplicate aggregate). Mapped to
/// <c>409 ProblemDetails</c> by the Api project's ExceptionHandler.
/// </summary>
public sealed class ConflictException(string message) : Exception(message)
{
}
