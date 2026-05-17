namespace Testers.SharedKernel.Abstractions;

// UnitOfWorkBehavior opens a transaction for these; queries skip it.
public interface ICommand<TResponse> : IRequest<TResponse>
{
}

public interface ICommand : ICommand<Unit>
{
}
