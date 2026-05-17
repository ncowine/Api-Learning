using Serilog;
using Testers.Api;
using Testers.Api.Endpoints;
using Testers.Api.Observability;
using Testers.Application;
using Testers.Infrastructure;

// Testers API — Host composition root.
//
// Reads as a manifest of what's wired in:
//   1. Serilog as the host logger (console + rolling file + correlation id enricher).
//   2. Application layer (handlers, validators, pipeline behaviors).
//   3. Infrastructure layer (EF Core, RabbitMQ, Redis, dispatcher, UoW, system clock).
//   4. API layer (auth schemes, exception handler, Swagger, endpoint scanner).
// Adding a new feature slice should NOT require touching this file: drop a class
// implementing IRequestHandler<,> and IEndpoint into a slice folder, and the scanners
// pick it up.

var builder = WebApplication.CreateBuilder(args);

// ---- Logging ----
builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Services(services);
    SerilogConfiguration.Configure(cfg, ctx);
});

// ---- DI composition ----
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

// ---- Middleware pipeline (order matters) ----
//
//   Serilog request logging   → one structured log per request with duration + status
//   CorrelationIdMiddleware   → reads/sets X-Correlation-Id, attaches to log scope
//   ExceptionHandler          → catches exceptions from anything below, maps to ProblemDetails
//   Swagger (dev only)        → /swagger/index.html
//   Authentication            → populates HttpContext.User from OktaJwt OR ApiKey scheme
//   Authorization             → enforces RequireAuthorization(...) policies on endpoints
//   Endpoints                 → MapDiscoveredEndpoints + the small set of root endpoints
//
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

// Root + health endpoints (no auth, no slice).
app.MapGet("/", () => Results.Ok(new { service = "Testers API", status = "running" }))
    .AllowAnonymous();
app.MapHealthChecks("/health/live").AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();

// Feature-slice endpoints under /api/v1, discovered from the Application assembly.
app.MapDiscoveredEndpoints();

app.Run();

// Exposed so WebApplicationFactory<Program> can locate the entry point in integration tests.
public partial class Program;
