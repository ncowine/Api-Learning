namespace Testers.Application.Abstractions;

/// <summary>
/// Base marker for everything dispatched through <see cref="IDispatcher"/>. Carries the response
/// type so the dispatcher and pipeline behaviors are strongly typed end-to-end.
///
/// Prefer the more specific <see cref="ICommand{TResponse}"/> (state-changing) or
/// <see cref="IQuery{TResponse}"/> (read-only) markers in feature slices, since pipeline behaviors
/// (UnitOfWork, Performance, etc.) filter by command-vs-query.
/// </summary>
public interface IRequest<TResponse>
{
}
