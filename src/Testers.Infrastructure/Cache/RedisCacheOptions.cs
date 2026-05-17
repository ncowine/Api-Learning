namespace Testers.Infrastructure.Cache;

public sealed class RedisCacheOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = "localhost:6379";
    public string InstanceName { get; init; } = "testers-api:";  // prefixed on every key
}
