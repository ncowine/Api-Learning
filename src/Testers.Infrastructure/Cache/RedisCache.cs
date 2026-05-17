using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testers.SharedKernel.Abstractions;

namespace Testers.Infrastructure.Cache;

// ICache over StackExchange.Redis. JSON values. GetOrAddAsync doesn't coalesce concurrent
// misses - if that matters use DataCache<,> from Cache/Library/ instead.
internal sealed class RedisCache(IConnectionMultiplexer redis, IOptions<RedisCacheOptions> options) : ICache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _db = redis.GetDatabase();
    private readonly string _prefix = options.Value.InstanceName;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(Prefixed(key));
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value!, JsonOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) =>
        _db.StringSetAsync(Prefixed(key), JsonSerializer.Serialize(value, JsonOptions), ttl);

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        _db.KeyDeleteAsync(Prefixed(key));

    public async Task<T> GetOrAddAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var value = await factory(ct);
        if (value is not null) await SetAsync(key, value, ttl, ct);
        return value!;
    }

    private string Prefixed(string key) => string.Concat(_prefix, key);
}
