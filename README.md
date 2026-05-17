# Testers API

.NET 8 API that replaces the data layer of an existing WPF tester app. Testers create test plans
(Plan / Category / SubCategory / Task), record actions per build with outcomes PASS / FAIL /
SKIP / BLOCK, and attach comments + bug links.

See [ARCHITECTURE.md](./ARCHITECTURE.md) for the why behind the design.

## Prerequisites

- .NET 8 SDK (pinned to 8.0.421 via `global.json`)
- Docker (for MySQL / RabbitMQ / Redis via `docker-compose.yml`)

## Quick start

```powershell
# Local infra
docker compose up -d

# Build
dotnet build

# Migrations (one per DbContext)
dotnet ef database update --project src/Testers.Infrastructure --startup-project src/Testers.Api --context AppDbContext
dotnet ef database update --project src/Testers.Infrastructure --startup-project src/Testers.Api --context TestPlanDbContext

# Run
dotnet run --project src/Testers.Api
```

API on `http://localhost:5150` / `https://localhost:7214`. Swagger opens at `/swagger`.
RabbitMQ UI at `http://localhost:15672` (guest/guest).

## Tests

```powershell
dotnet test tests/Testers.UnitTests           # no Docker, ~1s
dotnet test tests/Testers.IntegrationTests    # Testcontainers MySQL; Docker required
```

## Layout

```
src/
  Testers.Domain/         entities, value objects, domain events, smart enums
  Testers.Application/    feature slices + dispatcher + pipeline behaviors + exceptions
  Testers.Infrastructure/ EF Core, RabbitMQ, Redis, auth helpers, dispatcher impl
  Testers.Contracts/      published integration events (leaf project, no refs)
  Testers.Api/            Program.cs, auth schemes, swagger, exception handler
tests/
  Testers.UnitTests/        handler + domain tests (NSubstitute + Shouldly)
  Testers.IntegrationTests/ WebApplicationFactory + Testcontainers MySQL
```

Project refs (compiler-enforced): `Domain <- Application <- Infrastructure <- Api`.

## Adding a feature slice

One folder under `src/Testers.Application/Features/<Area>/<UseCase>/`:

```
DoSomethingRequest.cs    HTTP body DTO
DoSomethingCommand.cs    record : ICommand<DoSomethingResult>
DoSomethingResult.cs     response DTO
DoSomethingValidator.cs  AbstractValidator<DoSomethingCommand>
DoSomethingHandler.cs    internal sealed : IRequestHandler<DoSomethingCommand, DoSomethingResult>
DoSomethingEndpoint.cs   : IEndpoint
```

`AddApplication()` scans for the handler + validator; `EndpointScanner` maps the endpoint under
`/api/v1`. No central registration. See `Features/Execution/ActionTaskRun/` as the reference.

## Configuration

`src/Testers.Api/appsettings.json` overridden by `appsettings.Development.json` and env vars
(use `__` for nesting: `Database__ConnectionString`). Sections: `Database`, `Rabbit`, `Redis`,
`Okta`, `Serilog`.

## Stack

- .NET 8, minimal APIs, IExceptionHandler
- EF Core 8 + Pomelo MySQL
- RabbitMQ.Client 6.x with publisher confirms
- StackExchange.Redis + vendored CacheRepository (`src/Testers.Infrastructure/Cache/Library/`)
- FluentValidation 11 (last MIT version)
- Scrutor (assembly scanning)
- Serilog with correlation-id propagation
- xUnit + Shouldly + NSubstitute
- Testcontainers (integration tests)

All MIT / Apache 2.0 / BSD. See `Directory.Packages.props` for versions.
