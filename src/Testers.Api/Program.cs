// Testers API — Host composition root.
//
// Each architectural layer exposes a single `services.AddXxx(config)` extension that does
// its own DI registration, EF Core wiring, endpoint mapping, etc. Program.cs reads as a
// manifest of what is composed into this deployment. Adding a new feature slice should
// NOT require touching this file.

var builder = WebApplication.CreateBuilder(args);

// ---- Layer composition will be added by upcoming scaffolding tasks ----
// builder.Services.AddDomain();
// builder.Services.AddApplication();
// builder.Services.AddInfrastructure(builder.Configuration);
// builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// ---- Endpoint mapping (each feature slice contributes via extension methods, registered above) ----
app.MapGet("/", () => Results.Ok(new { service = "Testers API", status = "scaffolded" }));

app.Run();

// Exposed so WebApplicationFactory<Program> can locate the entry point in integration tests.
public partial class Program;
