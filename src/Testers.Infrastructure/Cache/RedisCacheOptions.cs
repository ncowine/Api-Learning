namespace Testers.Infrastructure.Cache;

/// <summary>
/// Bound from the <c>Redis</c> section of <c>appsettings.json</c>. Used by
/// <see cref="RedisCache"/> (the <see cref="Testers.Application.Abstractions.ICache"/> impl).
///
/// Sample <c>appsettings.json</c>:
/// <code>
/// "Redis": {
///   "ConnectionString": "localhost:6379",
///   "InstanceName":     "testers-api:"
/// }
/// </code>
/// </summary>
public sealed class RedisCacheOptions
{
    public const string SectionName = "Redis";

    /// <summary>StackExchange.Redis connection string. Comma-separated for Sentinel/cluster.</summary>
    public string ConnectionString { get; init; } = "localhost:6379";

    /// <summary>Prefix prepended to every cache key so multiple apps can share one Redis instance safely.</summary>
    public string InstanceName { get; init; } = "testers-api:";
}
