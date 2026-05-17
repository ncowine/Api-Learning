namespace Testers.Application.Exceptions;

// Api maps to 404.
public sealed class NotFoundException(string entity, object key)
    : Exception($"{entity} with key '{key}' was not found.")
{
    public string Entity { get; } = entity;
    public object Key { get; } = key;
}
