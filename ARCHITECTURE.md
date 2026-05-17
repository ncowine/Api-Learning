# Architecture

The "why" behind every architectural choice in this codebase. Skim the headings; dive into the
section that matches the question you have.

> 📂 For "how do I run it" / "how do I add a feature", see [`README.md`](./README.md).

---

## Reading map

| If you're trying to | Read |
|---|---|
| Understand the layers and what depends on what | [Layers + dependency rule](#layers--dependency-rule) |
| Find which database a row lives in | [Bounded contexts + databases](#bounded-contexts--databases) |
| Understand how atomic cross-DB writes work | [Cross-DB transaction model](#cross-db-transaction-model) |
| See how events get published to RabbitMQ reliably | [The outbox pattern](#the-outbox-pattern) |
| Add audit fields to a new entity | [Auditing + soft-delete](#auditing--soft-delete) |
| Wire authentication for a new endpoint | [Authentication + authorisation](#authentication--authorisation) |
| Understand the dispatcher + pipeline behaviors | [The dispatcher pipeline](#the-dispatcher-pipeline) |
| Cache something | [Caching](#caching) |
| Add a new feature slice | [Adding a feature slice](#adding-a-feature-slice) |
| Decide where a new file goes | [Folder organisation principle](#folder-organisation-principle) |

---

## Layers + dependency rule

Five `src/` projects, layered. The compiler enforces the dependency direction:

```
Domain (entities, events) ─────────── nothing
Application (handlers, behaviors) ──→ Domain
Infrastructure (EF, RabbitMQ, etc.) ─→ Application + Domain
Api (Host, auth, swagger) ──────────→ Infrastructure + Contracts (transitively the rest)
Contracts (DTOs other apps consume) ─ nothing
```

**Why layered, not Modular Monolith with per-context project bundles?** Considered and rejected.
Modular Monolith gives stronger boundaries (per-context Domain/Application/Infra/Contracts) but
costs ~20 projects. For a one-developer POC the ceremony outweighs the boundary value. The
single-project-per-layer + feature-folders inside `Application` gives 80 % of the boundary
benefit at 7 projects.

**Vertical slice inside `Application`:** each use-case is one folder containing all its artifacts
(Command / Handler / Validator / Endpoint / Request / Result). Adding a feature touches one
folder. No `Controllers/` / `Services/` / `Repositories/` / `DTOs/` to hunt across.

---

## Bounded contexts + databases

**Two physical databases, same MySQL server.**

| DB | Owner | Tables |
|---|---|---|
| **`testplan_db`** | Another team (shared with multiple apps) | TestPlan, Category, SubCategory, TaskDefinition, Game, Build, **TaskRun**, Comment, BugLink |
| **`app_db`** | This API (sole owner) | OutboxMessage, InboxMessage, AuditLog, ApiKey, read-model projections, app-specific metadata |

Two `DbContext`s reflect this split: `TestPlanDbContext` and `AppDbContext`. They share one
`MySqlConnection` per request (see next section).

**Why TaskRun lives in the shared DB**: every consuming app needs to see test-action history. If
TaskRun lived in our App DB only, we'd be publishing events that the other apps would have to
project into their own copy — extra coordination for no win. Audit fields (CreatedAt/By, etc.)
on TaskRun become visible to those apps too, which is useful.

**Why the outbox lives in our App DB only**: it's our operational concern. If it broke, our pager
would go off — putting the table in someone else's DB would mean explaining outbox failures to
their team. Cross-DB transactions handle the atomicity (next section).

**Cross-DB reads are NOT joins.** API composition: query each DB independently, cache the static
fields, merge in application code. Future-proofs if the other team ever fronts their DB with a
service.

---

## Cross-DB transaction model

```
┌─ HTTP request ────────────────────────────────────────────────────────────────────────┐
│                                                                                       │
│  UnitOfWorkBehavior (pipeline)                                                        │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐ │
│  │  SharedConnection.BeginTransactionAsync()                                        │ │
│  │  ┌────────────────────────────────────────────────────────────────────────────┐  │ │
│  │  │  Handler                                                                   │  │ │
│  │  │    testPlanDb.Set<TaskRun>().Add(run)                                      │  │ │
│  │  │    await testPlanDb.SaveChangesAsync(ct)                                   │  │ │
│  │  │      └─→ AuditInterceptor stamps Created/Modified                          │  │ │
│  │  │      └─→ OutboxInterceptor walks BOTH contexts, finds aggregate events,    │  │ │
│  │  │           queues OutboxMessage into appDb.ChangeTracker                    │  │ │
│  │  │      └─→ EF emits INSERT into testplan_db.task_run                         │  │ │
│  │  │                                                                            │  │ │
│  │  │    await appDb.SaveChangesAsync(ct)                                        │  │ │
│  │  │      └─→ EF emits INSERT into app_db.outbox_message                        │  │ │
│  │  └────────────────────────────────────────────────────────────────────────────┘  │ │
│  │  SharedConnection.CommitAsync()  ← both INSERTs commit atomically                │ │
│  └──────────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                       │
└───────────────────────────────────────────────────────────────────────────────────────┘
```

**Why it works**: same MySQL server. One `MySqlConnection` opens once per HTTP request,
both `DbContext`s pull from it, one `MySqlTransaction` wraps everything. Cross-DB writes
commit (or roll back) as one unit.

**Why this isn't XA / distributed transactions**: those exist for cross-server cases. We
explicitly aren't doing that. Same MySQL instance + same connection = one native transaction.

**Implementation**:
- `SharedConnection` (Infrastructure/Persistence/) — scoped per request, lazy-opens
- `UnitOfWorkBehavior` (Application/Behaviors/) — calls `IUnitOfWork.BeginAsync` for commands
- `UnitOfWork` impl (Infrastructure/Dispatching/) — calls `Database.UseTransactionAsync` on
  both contexts so EF participates in the shared transaction
- The two `DbContext`s are registered in DI with a factory that pulls the connection from
  `SharedConnection` synchronously (per scope; one open per request)

---

## The outbox pattern

Why: **publishing to RabbitMQ separately from the business write is unsafe**. If the broker
is down or the process crashes between the write and the publish, you've lost an event. The
outbox makes the event publishable durably alongside the business write.

```
Aggregate raises IDomainEvent
        │
        ▼
SaveChangesAsync                     OutboxPublisher BackgroundService
   OutboxInterceptor                          │
   serialises event                           │
   inserts OutboxMessage row                  │  poll every 1s
        │                                     ▼
        ▼                              SELECT WHERE processed_at IS NULL
   Same transaction as business write         │
   (atomic via SharedConnection)              ▼
        │                              channel.BasicPublish(...)
        ▼                              channel.WaitForConfirmsOrDie(5s)
   COMMIT  ────────► durable row ◄───  UPDATE outbox SET processed_at = NOW()
                          │
                          ▼
                    RabbitMQ exchange "testers.events.v1"
                          │
                          ▼
                    Routing key e.g. "task.run.recorded.v1"
                          │
                          ▼
                    Downstream consumers (read-model updaters,
                    notifications, analytics, other apps)
```

Properties this gives you:

| Failure mode | Outcome |
|---|---|
| Process crash *before* SaveChanges | Nothing committed → nothing lost. |
| Process crash *after* SaveChanges but before publish | Row is durable, unprocessed. Next poll publishes. |
| RabbitMQ down | Polls fail, row stays unprocessed. Recovers when broker is back. |
| Broker accepts publish then dies before persisting | `WaitForConfirmsOrDie` throws → row stays unprocessed → retried. |
| Consumer processes event twice | Idempotency via `MessageId = OutboxMessage.Id` + inbox pattern. |

**Routing key convention**: `<aggregate>.<event>.v<N>` derived from the event class name.
`TaskRunRecorded` → `task.run.recorded.v1`. Versioned in the routing key so v2 consumers can
coexist with v1 consumers during migrations.

**Single-instance** for now (single process polls the table). To scale out: switch the `SELECT`
to `... FOR UPDATE SKIP LOCKED` (MySQL 8+) so multiple instances claim disjoint slices.

**Inbox pattern for incoming events** (when we consume from other apps): each consumer keeps a
`(MessageId, ConsumerName)` row in `app_db.inbox_message`. Check before processing; insert
inside the same transaction as the business write. Gives exactly-once *effect* on top of
RabbitMQ's at-least-once delivery. ConsumerHost wiring is deferred until we have an actual
incoming event to consume.

---

## Auditing + soft-delete

Two interfaces on entities, one interceptor:

- **`IAuditable`** — `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`. Stamped by
  `AuditInterceptor` from `ICurrentUser` + `IClock` at `SaveChangesAsync` time. Every
  `AggregateRoot<TId>` automatically implements this.
- **`ISoftDeletable`** — `IsDeleted`, `DeletedAt`, `DeletedBy`. Marker that converts
  `EntityState.Deleted` to `Modified` and flips the flags. Concrete entities that need it
  implement the interface directly.

Mixed policy (per user preference): soft-delete user-facing entities (TaskRun, TestPlan,
Comment); hard-delete transient ones (session tokens, scratch metadata). Both kinds get an
`AuditLog` row recording who and what (cross-DB AuditLog writes deferred to a follow-up; the
entity + interceptor stamping logic are in place).

---

## Authentication + authorisation

Two schemes, both authenticate the same `ICurrentUser` surface:

| Scheme | For | Implementation |
|---|---|---|
| **Okta JWT** (`AuthSchemes.OktaJwt`) | Humans (testers using the web/WPF UI) | `AddJwtBearer` reads `Okta` config, validates against Okta JWKS, populates `ClaimsPrincipal`. |
| **API key** (`AuthSchemes.ApiKey`) | Services (CI, partner integrations) | `ApiKeyAuthenticationHandler` reads `X-Api-Key`, hashes with SHA-256, looks up in `app_db.api_key` (unique index on hash, sub-ms). |

Both project into `ICurrentUser` via `HttpContextCurrentUser`:

```
HttpContext.User    ──projected by──►    ICurrentUser
  sub claim         ──►   .Id            (e.g. "okta:user-42" or "apikey:{guid}")
  name claim        ──►   .DisplayName
  user:kind claim   ──►   .Kind          (Human | Service)
  role claims       ──►   .Roles         (Okta groups OR API-key scopes)
```

Handlers, interceptors, and the AuditInterceptor consume `ICurrentUser` only — they never
branch on "is this Okta or an API key?" That decision lives in one place (the auth handler).

**Authorisation policies** (`AuthorizationPolicies.cs`):
- `AnyAuthenticated` — default; either scheme accepted
- `HumanOnly` — for endpoints that need a real user (e.g. submitting a bug)
- `ServiceOnly` — for admin-y bulk endpoints meant for CI

`RequireAuthorization()` (no name) uses `AnyAuthenticated`. `RequireAuthorization("HumanOnly")`
narrows.

---

## The dispatcher pipeline

Hand-rolled to avoid MediatR's commercial licence (v11+). ~150 lines total across abstractions
+ impl. Source-generated alternatives (`martinothamar/Mediator`) exist; we picked the manual
path so the wiring is obvious in code.

```
IDispatcher.Send<TResponse>(IRequest<TResponse> request)
         │
         ▼
[LoggingBehavior]                ← structured log: request name, user, elapsed ms, success/fail
         │
         ▼
[ValidationBehavior]             ← runs FluentValidation IValidator<TRequest> → ValidationException
         │
         ▼
[UnitOfWorkBehavior]             ← commands only: begins shared MySQL transaction
         │
         ▼
[PerformanceBehavior]            ← warns if handler exceeds 500 ms (catches slow queries, N+1)
         │
         ▼
IRequestHandler<TRequest, TResponse>.Handle(...)
```

Behaviors are registered open-generic in `AddApplication`; the dispatcher uses reflection to
resolve them per request. Reflection cost is microseconds — irrelevant next to DB I/O. Switch
to compiled-expression caches per `(TRequest, TResponse)` pair if profiling ever shows it
as a hot path.

**Command vs Query**: `UnitOfWorkBehavior` opens a transaction only when `request is ICommand<_>`.
Queries skip the cost. Marker interfaces in `Application/Abstractions/`.

---

## Caching

Two patterns, used where each fits:

### `ICache` (generic, Redis)
- Application abstraction in `Application/Abstractions/ICache.cs`
- Implementation: `RedisCache` (`Infrastructure/Cache/`) using StackExchange.Redis
- Surface: `Get/Set/Remove/GetOrAddAsync<T>(key, ...)`
- For: cross-instance, ad-hoc, string-keyed values
- Limitation: `GetOrAddAsync` doesn't coalesce concurrent misses (use the next pattern when
  that matters)

### `DataCache<TKey, TValue>` (typed, in-memory, coalescing)
- Vendored from [github.com/ncowine/CacheRepository](https://github.com/ncowine/CacheRepository)
  into `Infrastructure/Cache/Library/`
- Subclass per use case; implement two `FetchAsync` methods (single + batch)
- Killer feature: concurrent-fetch coalescing — N parallel `Get(sameKey)` triggers ONE
  `FetchAsync`. Worth it for hot read paths where many concurrent requests miss on the same
  TaskDefinition / Build / etc.
- In-memory: single-instance only (use Redis for cross-instance shared state)

See `Cache/Library/NOTICE.md` for vendoring details + how to update from upstream.

---

## Observability

- **Serilog** as the host logger. Console + rolling-file (`logs/testers-api-{date}.log`) by
  default. Sinks/levels overrideable via `appsettings.json`'s `Serilog` section.
- **Correlation id**: `CorrelationIdMiddleware` reads/sets `X-Correlation-Id` header,
  pushes it to `Serilog.LogContext`. Every log line in the request scope is enriched
  automatically — including DB queries (via EF Core logging) and outbox publish messages
  (the publisher carries the originating correlation id on the AMQP `correlation-id` property).
- **Enrichers**: MachineName, ProcessId, ThreadId, Application name, Environment.
- **`SerilogRequestLogging` middleware**: one structured log per HTTP request with timing +
  status code.

Metrics + tracing (OpenTelemetry) wiring is intentionally not yet in this scaffold — add
when there's a real dashboard to push to.

---

## Adding a feature slice

Example: a new `CompleteTestPlan` command in the `Catalog` area.

```
src/Testers.Application/Features/Catalog/CompleteTestPlan/
  CompleteTestPlanRequest.cs    record CompleteTestPlanRequest(Guid GameBuildId)
  CompleteTestPlanCommand.cs    record CompleteTestPlanCommand(Guid TestPlanId, Guid GameBuildId) : ICommand<Unit>
  CompleteTestPlanResult.cs     (omit if returning Unit)
  CompleteTestPlanValidator.cs  AbstractValidator<CompleteTestPlanCommand>
  CompleteTestPlanHandler.cs    internal sealed : IRequestHandler<CompleteTestPlanCommand, Unit>
  CompleteTestPlanEndpoint.cs   public sealed class ... : IEndpoint
```

`AddApplication()` auto-registers the handler (via Scrutor scanning) and validator (via
FluentValidation's assembly scan). `EndpointScanner` auto-maps the endpoint under `/api/v1`.

Domain types the handler needs (entities, value objects, smart enums) go in
`src/Testers.Domain/Catalog/`. Persistence wiring (DbSet, entity configuration) goes in
`src/Testers.Infrastructure/Persistence/{App,TestPlan}DbContext.OnModelCreating`.

**See `Features/Execution/ActionTaskRun/` for the reference example covering all of this
end-to-end.**

---

## Folder organisation principle

> **A folder exists to group 2+ siblings of the same category. Singletons sit at the parent
> level — no wrapping folder.**

Applied to projects (no `Host/` for a single API project, no `Modules/` for a flat layered
structure) and applied inside projects (`Abstractions/`, `Behaviors/`, `Features/`,
`Persistence/`, `Messaging/`, `Cache/`, `Auth/`, etc., but `Program.cs` /
`DependencyInjection.cs` / `SystemClock.cs` at project root because they're singletons).

Folder paths become namespace paths by default. Folder structure = namespace structure =
mental model of what is where.

---

## Decisions log (the "why we didn't")

| Considered | Rejected because |
|---|---|
| Modular Monolith (per-context project bundles) | Project explosion for one-developer POC. Came back to flat layered. |
| MediatR | Commercial licence from v12. Hand-rolled IDispatcher is ~150 lines, no surprises. |
| FluentAssertions v8+ | Commercial licence. Stuck with Shouldly (MIT). |
| Moq | License/funding drama. NSubstitute (BSD) is equivalent. |
| MassTransit | Moving commercial in v9. Used raw RabbitMQ.Client + a thin wrapper. |
| Repository pattern (`ITaskRunRepository`) | DbContext is already a repository. Handlers inject `IAppDbContext` / `ITestPlanDbContext` directly. |
| Cross-DB JOINs | Couples our app to the other team's schema. API composition + caching instead. |
| Generic `Repository<T>` base | Speculative abstraction. Added when 3+ slices want the same thing. |
| Domain Events via in-process notifications | Hard to make atomic. Outbox-via-interceptor is bulletproof. |
| `IEventPublisher` in Application | Had it; turned out to have no callers (OutboxPublisher works with `OutboxMessage` rows directly). Removed. |
| BCrypt for API keys | API keys are 256-bit random tokens. SHA-256 is sufficient + fast. |
| ConsumerHost skeleton without consumers | Scaffolding-by-faith. Add when the first concrete incoming integration event arrives. |
| `SELECT ... FOR UPDATE SKIP LOCKED` in outbox | Single-instance for now. Add when scaling out. |
| OpenTelemetry tracing | Add when there's a real backend to push to. Don't pay the complexity cost speculatively. |
| Strict Clean Architecture (Domain → Application interfaces for DbContext only) | We do this for `IAppDbContext` / `ITestPlanDbContext`. But Application references EF Core directly (via the `Microsoft.AspNetCore.App` framework reference). Pragmatic; testable. |

---

## Where to look when …

| Question | File / folder |
|---|---|
| How is a handler dispatched? | `Infrastructure/Dispatching/Dispatcher.cs` |
| How are domain events captured? | `Infrastructure/Outbox/OutboxInterceptor.cs` |
| How are events published to RabbitMQ? | `Infrastructure/Messaging/OutboxPublisher.cs` |
| How are audit fields stamped? | `Infrastructure/Audit/AuditInterceptor.cs` |
| How does cross-DB transaction work? | `Infrastructure/Persistence/SharedConnection.cs` + `Dispatching/UnitOfWork.cs` |
| How is `ICurrentUser` populated from HTTP? | `Api/Auth/HttpContextCurrentUser.cs` |
| How does API-key auth work? | `Api/Auth/ApiKeyAuthenticationHandler.cs` |
| How does Program.cs compose everything? | `Api/Program.cs` (~50 lines, reads as a manifest) |
| Example of a feature slice | `Application/Features/Execution/ActionTaskRun/` |
| Example of a domain aggregate | `Domain/Execution/TaskRun.cs` |
| Example of unit tests | `tests/Testers.UnitTests/` |
