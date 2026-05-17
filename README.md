# Testers API

A .NET 8 API replacing the data-access logic of an existing WPF tester app. Testers create
TestPlans (Plan → Category → SubCategory → Task), action tasks per game build with outcomes
(PASS / FAIL / SKIP / BLOCK), and attach comments + bug links. Designed for **1000s of
concurrent users** with audit, RabbitMQ event publishing, multi-DB transactions, and Okta +
API-key authentication.

> 📐 For the "why" behind every architectural choice, read [`ARCHITECTURE.md`](./ARCHITECTURE.md).

---

## Prerequisites

- **.NET 8 SDK** (pinned via `global.json` to `8.0.421`)
- **Docker Desktop** (for local MySQL, RabbitMQ, Redis via `docker-compose.yml`)
- Visual Studio 2022 / Rider / VS Code with C# Dev Kit (any works)

---

## Quick start

```powershell
# 1. Start local infrastructure (MySQL on 3306, RabbitMQ on 5672 + UI on 15672, Redis on 6379)
docker compose up -d

# 2. Build
dotnet build

# 3. Apply EF Core migrations (creates the app_db + testplan_db schemas)
# The migrations command can run on either DbContext separately.
dotnet ef database update --project src/Testers.Infrastructure --startup-project src/Testers.Api --context AppDbContext
dotnet ef database update --project src/Testers.Infrastructure --startup-project src/Testers.Api --context TestPlanDbContext

# 4. Run the API
dotnet run --project src/Testers.Api
```

The API listens on `http://localhost:5150` and `https://localhost:7214` by default. Swagger UI
opens automatically at `/swagger`.

RabbitMQ management UI is at `http://localhost:15672` (login: `guest` / `guest`).

---

## Running tests

```powershell
# Unit tests (no Docker dependency, ~1 second)
dotnet test tests/Testers.UnitTests

# Integration tests (Testcontainers spins up an ephemeral MySQL; Docker must be running)
dotnet test tests/Testers.IntegrationTests
```

---

## Repo layout

```
src/
  Testers.Domain/         entities, value objects, domain events, smart enums
  Testers.Application/    feature slices, dispatcher, pipeline behaviors, exceptions
  Testers.Infrastructure/ DbContexts, interceptors, outbox, RabbitMQ, Redis, auth, dispatcher impl
  Testers.Contracts/      published integration events + DTOs other services consume
  Testers.Api/            Host (Program.cs) — composes everything; auth schemes; swagger
tests/
  Testers.UnitTests/        handler + domain tests using NSubstitute + Shouldly
  Testers.IntegrationTests/ WebApplicationFactory + Testcontainers MySQL
```

The dependency rule (enforced by project references):

```
Domain  ←  Application  ←  Infrastructure  ←  Api
                                   ←        ←
                            (Contracts is a leaf — no project refs)
```

---

## Adding a new feature slice

For a feature called `DoSomething` in the `Catalog` area, create one folder:

```
src/Testers.Application/Features/Catalog/DoSomething/
  DoSomethingRequest.cs    ← HTTP body DTO
  DoSomethingCommand.cs    ← record : ICommand<DoSomethingResult>
  DoSomethingResult.cs     ← response DTO
  DoSomethingValidator.cs  ← AbstractValidator<DoSomethingCommand>
  DoSomethingHandler.cs    ← internal sealed : IRequestHandler<DoSomethingCommand, DoSomethingResult>
  DoSomethingEndpoint.cs   ← : IEndpoint
```

`AddApplication()` auto-registers the handler and validator. `EndpointScanner` auto-maps the
endpoint under `/api/v1`. **Zero central registration to touch.** See
`Features/Execution/ActionTaskRun/` for a complete reference example.

---

## Configuration

Settings live in `src/Testers.Api/appsettings.json` and are overridden by:
- `appsettings.Development.json` for local dev
- Environment variables (use `__` for nesting, e.g. `Database__ConnectionString`)

Sections:
- `Database` — MySQL connection string + AppSchema / TestPlanSchema names
- `Rabbit` — broker URI, exchange name, outbox poll cadence
- `Redis` — connection string + key prefix
- `Okta` — Authority + Audience + RequireHttpsMetadata
- `Serilog` — minimum log levels per source

---

## Tech stack

- **.NET 8** (LTS), minimal APIs, `IExceptionHandler`
- **EF Core 8** + **Pomelo MySQL** provider
- **RabbitMQ.Client** 6.x with publisher confirms + delayed-message plugin support
- **StackExchange.Redis** + vendored
  [CacheRepository](./src/Testers.Infrastructure/Cache/Library/NOTICE.md) for typed in-memory
  caches with concurrent-fetch coalescing
- **FluentValidation** 11 (last MIT version) for command validation
- **Scrutor** for assembly scanning (handlers, validators, endpoints)
- **Serilog** with correlation-id enrichment, console + rolling-file sinks
- **xUnit** + **Shouldly** + **NSubstitute** for tests
- **Testcontainers** for integration tests against ephemeral MySQL

License-conscious: every dependency is MIT / Apache 2.0 / BSD. See
`Directory.Packages.props` for the full version-pinned list.

---

## Further reading

- [`ARCHITECTURE.md`](./ARCHITECTURE.md) — the architectural decisions, layer by layer
- [`src/Testers.Infrastructure/Cache/Library/NOTICE.md`](./src/Testers.Infrastructure/Cache/Library/NOTICE.md) — about the vendored CacheRepository
