namespace Testers.Application.Abstractions;

/// <summary>
/// Read-through cache abstraction. Backed by Redis in production (via the user's CacheRepository
/// library); a fake in-memory implementation in tests. Keys are strings; values are serialised
/// JSON. TTL is required on writes so stale data eventually expires even without explicit
/// invalidation.
/// </summary>
public interface ICache
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>Get the value for <paramref name="key"/>, or compute and cache via <paramref name="factory"/> if missing.</summary>
    Task<T> GetOrAddAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default);
}
