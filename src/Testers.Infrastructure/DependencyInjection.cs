using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MySqlConnector;
using StackExchange.Redis;
using Testers.SharedKernel.Abstractions;
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

        services.AddDbContext<AppDbContext>((sp, opts) => ConfigureDbContext(sp, opts, nameof(AppDbContext)));
        services.AddDbContext<TestPlanDbContext>((sp, opts) => ConfigureDbContext(sp, opts, nameof(TestPlanDbContext)));

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
            var opts = sp.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
            return ConnectionMultiplexer.Connect(opts.ConnectionString);
        });
        services.AddSingleton<ICache, RedisCache>();

        return services;
    }

    private static void ConfigureDbContext(IServiceProvider sp, DbContextOptionsBuilder options, string contextName)
    {
        var shared = sp.GetRequiredService<SharedConnection>();
        var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var connection = shared.GetOpenAsync().AsTask().GetAwaiter().GetResult();

        switch (dbOptions.Provider)
        {
            case DbProvider.Sqlite:
                // Per-context migrations history table so both contexts share one SQLite file
                // without colliding on the default __EFMigrationsHistory.
                options.UseSqlite(connection, b =>
                    b.MigrationsHistoryTable($"__ef_migrations_{contextName.ToLowerInvariant()}"));
                break;
            case DbProvider.MySql:
                // Pin to 8.0.30 for now; flip to ServerVersion.AutoDetect against a real instance.
                options.UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 30)));
                break;
            default:
                throw new InvalidOperationException($"Unknown DbProvider: {dbOptions.Provider}");
        }

        options.AddInterceptors(
            sp.GetRequiredService<AuditInterceptor>(),
            sp.GetRequiredService<OutboxInterceptor>());
    }
}
