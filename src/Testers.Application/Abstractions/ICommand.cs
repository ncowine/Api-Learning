namespace Testers.Application.Abstractions;

/// <summary>
/// A request that modifies state. The UnitOfWorkBehavior opens a transaction for commands;
/// queries don't pay that cost. Use <see cref="ICommand{TResponse}"/> for any handler that
/// writes to a DbContext or raises a domain event.
/// </summary>
public interface ICommand<TResponse> : IRequest<TResponse>
{
}

/// <summary>Command with no meaningful return value — use <see cref="Unit"/> as the response type.</summary>
public interface ICommand : ICommand<Unit>
{
}
