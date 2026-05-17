namespace Testers.Application.Abstractions;

// Prefer ICommand<T> / IQuery<T> in slices — behaviors filter on those.
public interface IRequest<TResponse>
{
}
