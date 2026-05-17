using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testers.Application.Abstractions;

namespace Testers.Infrastructure.Cache;

/// <summary>
/// <see cref="ICache"/> implementation backed by StackExchange.Redis. Values are serialised to
/// JSON; keys are prefixed with <see cref="RedisCacheOptions.InstanceName"/> so multiple apps
/// can share one Redis instance safely. TTL is required on writes.
///
/// Limitation: <see cref="GetOrAddAsync"/> does NOT coalesce concurrent misses on the same key
/// (two simultaneous misses on key "X" will both run the factory). For hot-path coalescing,
/// use <c>DataCache&lt;TKey, TValue&gt;</c> from the vendored CacheRepository library instead
/// — it's in-memory but has elegant Lazy-based coalescing.
/// </summary>
internal sealed class RedisCache(IConnectionMultiplexer redis, IOptions<RedisCacheOptions> options) : ICache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _db = redis.GetDatabase();
    private readonly string _prefix = options.Value.InstanceName;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(Prefixed(key)).ConfigureAwait(false);
        if (value.IsNullOrEmpty)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value!, JsonOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return _db.StringSetAsync(Prefixed(key), json, ttl);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        return _db.KeyDeleteAsync(Prefixed(key));
    }

    public async Task<T> GetOrAddAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var cached = await GetAsync<T>(key, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(ct).ConfigureAwait(false);
        if (value is not null)
        {
            await SetAsync(key, value, ttl, ct).ConfigureAwait(false);
        }

        return value!;
    }

    private string Prefixed(string key) => string.Concat(_prefix, key);
}
