using Serilog;
using Testers.Api;
using Testers.Api.Endpoints;
using Testers.Api.Observability;
using Testers.Application;
using Testers.Infrastructure;

// Composition root. Adding a feature slice should not require touching this file -
// scanners pick up IRequestHandler<,> and IEndpoint impls from the Application assembly.

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Services(services);
    SerilogConfiguration.Configure(cfg, ctx);
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

// Middleware order matters:
//   Serilog request logging -> CorrelationId -> ExceptionHandler -> Swagger (dev) ->
//   Authentication -> Authorization -> endpoints.
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Root + health (no auth).
app.MapGet("/", () => Results.Ok(new { service = "Testers API", status = "running" })).AllowAnonymous();
app.MapHealthChecks("/health/live").AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();

// Feature slices under /api/v1 (scanner finds IEndpoint impls in Application).
app.MapDiscoveredEndpoints();

app.Run();

// For WebApplicationFactory<Program> in integration tests.
public partial class Program;
