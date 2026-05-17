namespace Testers.Application.Exceptions;

/// <summary>
/// Thrown when a handler is asked to operate on an entity that doesn't exist.
/// Mapped to <c>404 ProblemDetails</c> by the Api project's ExceptionHandler.
/// </summary>
public sealed class NotFoundException(string entity, object key)
    : Exception($"{entity} with key '{key}' was not found.")
{
    public string Entity { get; } = entity;

    public object Key { get; } = key;
}
