using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;

namespace Testers.Api.Observability;

/// <summary>
/// Configures Serilog as the host logger. Reads any extra config from <c>appsettings.json</c>'s
/// <c>Serilog</c> section (sinks, overrides) — letting ops tune log levels per environment
/// without code changes — then layers in our standard enrichers and a console + rolling-file
/// sink as defaults.
/// </summary>
public static class SerilogConfiguration
{
    public static void Configure(LoggerConfiguration cfg, WebHostBuilderContext ctx)
    {
        cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "Testers.Api")
            .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/testers-api-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}");
    }
}
