namespace Testers.SharedKernel.Abstractions;

// Redis-backed in prod; values serialised as JSON. TTL required so things eventually expire.
// Note: GetOrAddAsync doesn't coalesce concurrent misses - use a typed in-memory cache
// (e.g. DataCache<,> from the vendored CacheRepository) when single-flight matters.
public interface ICache
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<T> GetOrAddAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken ct = default);
}
