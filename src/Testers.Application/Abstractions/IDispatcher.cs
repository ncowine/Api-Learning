namespace Testers.Application.Abstractions;

/// <summary>
/// Entry point for sending requests through the pipeline (behaviors + handler).
/// Endpoints inject <see cref="IDispatcher"/> and call <see cref="Send{TResponse}"/>; they
/// never resolve handlers directly. Implementation lives in Infrastructure (so it can resolve
/// types from <c>IServiceProvider</c> at request time).
/// </summary>
public interface IDispatcher
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}
