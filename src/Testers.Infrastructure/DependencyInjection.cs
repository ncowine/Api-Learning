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

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Options.
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<RabbitOptions>(configuration.GetSection(RabbitOptions.SectionName));
        services.Configure<RedisCacheOptions>(configuration.GetSection(RedisCacheOptions.SectionName));

        // Persistence: shared connection + both DbContexts + interceptors.
        services.AddScoped<SharedConnection>();
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<OutboxInterceptor>();

        // MySQL 8.0.30 pinned for now; integration tests can flip to ServerVersion.AutoDetect.
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));
        services.AddDbContext<AppDbContext>((sp, options) => ConfigureMySql(sp, options, serverVersion));
        services.AddDbContext<TestPlanDbContext>((sp, options) => ConfigureMySql(sp, options, serverVersion));

        // Expose DbContexts via Application interfaces so handlers don't reference Infrastructure types.
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ITestPlanDbContext>(sp => sp.GetRequiredService<TestPlanDbContext>());

        // Dispatcher pipeline + clock + fallback current user (Api overrides ICurrentUser).
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, SystemCurrentUser>();

        // RabbitMQ.
        services.AddSingleton<RabbitConnectionFactory>();
        services.AddHostedService<OutboxPublisher>();

        // Redis. Typed in-memory caches use DataCache<,> from Cache/Library/ directly.
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
        // Sync open in DI factory: once per scope (HTTP request). Known compromise for
        // sharing one DbConnection across both DbContexts.
        var connection = shared.GetOpenAsync().AsTask().GetAwaiter().GetResult();
        options.UseMySql(connection, version);
        options.AddInterceptors(
            sp.GetRequiredService<AuditInterceptor>(),
            sp.GetRequiredService<OutboxInterceptor>());
    }
}
