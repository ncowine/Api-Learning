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
  Testers.SharedKernel/   generic CQS framework: abstractions + pipeline behaviors + exceptions
  Testers.Application/    feature slices + DI composition (uses SharedKernel)
  Testers.Infrastructure/ EF Core, RabbitMQ, Redis, auth helpers, dispatcher impl
  Testers.Contracts/      published wire DTOs + route constants (leaf project, for clients)
  Testers.Api/            Program.cs, auth schemes, swagger, exception handler
tests/
  Testers.UnitTests/        handler + domain tests (NSubstitute + Shouldly)
  Testers.IntegrationTests/ WebApplicationFactory + Testcontainers MySQL
```

Project refs (compiler-enforced):
- Domain, SharedKernel, Contracts are leaves
- Application -> Domain + Contracts + SharedKernel
- Infrastructure -> Application + SharedKernel
- Api -> Infrastructure + Contracts + SharedKernel

`Testers.SharedKernel` holds the generic plumbing - `IDispatcher`, `IRequest/ICommand/IQuery`,
`IPipelineBehavior`, the four behaviors (Logging/Validation/UoW/Performance), the four
exception types, `IClock`/`ICurrentUser`/`ICache`/`IUnitOfWork`/`IEndpoint`/`Unit`, plus the
`IAppDbContext`/`ITestPlanDbContext` marker interfaces. Nothing app-specific - reusable for
any .NET API with the same dispatcher pattern.

## Adding a feature slice

Wire shape (anything a client would consume) goes in `Testers.Contracts`. Server-only
artifacts (Command, Validator, Handler, Endpoint) go in `Testers.Application`.

```
src/Testers.Contracts/<Area>/
  DoSomethingRequest.cs    HTTP body record
  DoSomethingResult.cs     response record
  Routes.cs                add route template + URL builder for this endpoint

src/Testers.Application/Features/<Area>/DoSomething/
  DoSomething.cs           Command record (: ICommand<Result>) + Validator class
  DoSomethingHandler.cs    internal sealed : IRequestHandler<Command, Result>
  DoSomethingEndpoint.cs   : IEndpoint; uses Routes.<Area>.SomethingTemplate, binds Request
```

`AddApplication()` scans for the handler + validator. `EndpointScanner` maps the endpoint under
`/api/v1`. No central registration. See `Features/Execution/ActionTaskRun/` + the matching
`Testers.Contracts/Execution/` for the reference.

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
