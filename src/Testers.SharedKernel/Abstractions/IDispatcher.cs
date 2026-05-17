namespace Testers.SharedKernel.Abstractions;

// Endpoints inject this and call Send. Impl in Infrastructure (needs IServiceProvider).
public interface IDispatcher
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}
