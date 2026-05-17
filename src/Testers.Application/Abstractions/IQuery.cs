namespace Testers.Application.Abstractions;

// Read-only; UoW behavior skips opening a transaction. Use AsNoTracking + project to DTOs.
public interface IQuery<TResponse> : IRequest<TResponse>
{
}
