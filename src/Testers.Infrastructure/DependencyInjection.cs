using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using StackExchange.Redis;
using Testers.Application.Abstractions;
using Testers.Infrastructure.Audit;
using Testers.Infrastructure.Cache;
using Testers.Infrastructure.Dispatching;
using Testers.Infrastructure.Messaging;
using Testers.Infrastructure.Outbox;
using Testers.Infrastructure.Persistence;

namespace Testers.Infrastructure;

/// <summary>
/// Composes the Infrastructure layer into DI. Called once from <c>Program.cs</c>:
/// <c>services.AddInfrastructure(builder.Configuration)</c>.
///
/// Sub-step (a) wires: options, shared MySQL connection, both DbContexts, audit + outbox
/// interceptors, dispatcher, unit-of-work, system clock, and a fallback ICurrentUser.
/// Messaging (RabbitMQ) and Cache (Redis) wiring land in sub-steps (b) and (c). The HTTP-aware
/// ICurrentUser arrives with the Api project (task #6).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // ---- Options ----
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        // ---- Per-request shared MySqlConnection (both DbContexts pull from this) ----
        services.AddScoped<SharedConnection>();

        // ---- Interceptors (scoped: they capture per-request services like ICurrentUser) ----
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<OutboxInterceptor>();

        // ---- DbContexts ----
        // Pin to MySQL 8.0.30 for now; integration tests will swap to ServerVersion.AutoDetect.
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));

        services.AddDbContext<AppDbContext>((sp, options) => ConfigureMySql(sp, options, serverVersion));
        services.AddDbContext<TestPlanDbContext>((sp, options) => ConfigureMySql(sp, options, serverVersion));

        // Expose the DbContexts via the IAppDbContext / ITestPlanDbContext interfaces so
        // Application handlers can inject them without referencing concrete Infrastructure types.
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ITestPlanDbContext>(sp => sp.GetRequiredService<TestPlanDbContext>());

        // ---- Dispatcher + UoW + clock + fallback current user ----
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, SystemCurrentUser>();

        // ---- Messaging (sub-step b): RabbitMQ outbox publisher ----
        services.Configure<RabbitOptions>(configuration.GetSection(RabbitOptions.SectionName));
        services.AddSingleton<RabbitConnectionFactory>();
        services.AddHostedService<OutboxPublisher>();

        // ---- Cache (sub-step c): Redis-backed generic ICache ----
        // The vendored CacheRepository library (Cache/Library/) is available separately for
        // typed per-entity caches with concurrent-fetch coalescing — feature slices subclass
        // DataCache<TKey,TValue> and register their cache class directly.
        services.Configure<RedisCacheOptions>(configuration.GetSection(RedisCacheOptions.SectionName));
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisCacheOptions>>().Value;
            return ConnectionMultiplexer.Connect(opts.ConnectionString);
        });
        services.AddSingleton<ICache, RedisCache>();

        return services;
    }

    private static void ConfigureMySql(IServiceProvider sp, DbContextOptionsBuilder options, MySqlServerVersion version)
    {
        var shared = sp.GetRequiredService<SharedConnection>();
        // Sync open in the DI factory: happens once per scope (per HTTP request). Adding the
        // ConfigureAwait isn't useful here because GetAwaiter().GetResult() does the wait itself;
        // this is a known and accepted compromise for sharing one DbConnection across DbContexts.
        var connection = shared.GetOpenAsync().AsTask().GetAwaiter().GetResult();
        options.UseMySql(connection, version);
        options.AddInterceptors(
            sp.GetRequiredService<AuditInterceptor>(),
            sp.GetRequiredService<OutboxInterceptor>());
    }
}
