# Architecture

Notes on the design choices. See [README.md](./README.md) for run/build/add-a-slice.

## Layers

Five `src/` projects, layered. Compiler-enforced dependency direction:

```
Domain          (entities, events)              -> nothing
Application     (handlers, behaviors)           -> Domain
Infrastructure  (EF Core, RabbitMQ, Redis)      -> Application + Domain
Api             (Program.cs, auth, swagger)     -> Infrastructure (transitively the rest)
Contracts       (published DTOs)                -> nothing
```

Vertical slice inside Application: one folder per use-case with all artifacts (Command, Handler,
Validator, Endpoint, Request, Result). No `Services/`/`Repositories/`/`DTOs/` to hunt across.

Considered Modular Monolith (per-context project bundles): ~20 projects, too much ceremony for
one developer. The layered + feature-folders approach gets most of the boundary value at 7 projects.

## Bounded contexts + databases

Two databases on the same MySQL server:

| DB | Owner | Holds |
|---|---|---|
| `testplan_db` | Another team's app (shared) | TestPlan, Category, SubCategory, TaskDefinition, Game, Build, **TaskRun**, Comment, BugLink |
| `app_db` | This API (sole) | OutboxMessage, InboxMessage, AuditLog, ApiKey, projections, app metadata |

Two DbContexts: `TestPlanDbContext` and `AppDbContext`. They share one `MySqlConnection` per
request via `SharedConnection` so cross-DB writes can sit in one native transaction (next section).

TaskRun lives in the shared DB so other apps see the test-action history directly. If it were in
our App DB only, every consumer would have to project our events into their own copy.

Outbox lives in App DB only. It's our operational concern; running it from someone else's DB
would mean explaining outage alerts to their team.

Cross-DB *reads* are API composition (separate queries + cache + in-memory merge), not joins.
Keeps us decoupled if the other team ever puts a service in front of their DB.

## Cross-DB transaction model

```
HTTP request
  UnitOfWorkBehavior (pipeline)
    SharedConnection.BeginTransactionAsync()
      Handler:
        testPlanDb.Set<TaskRun>().Add(run)
        await testPlanDb.SaveChangesAsync(ct)
          -> AuditInterceptor stamps Created/Modified
          -> OutboxInterceptor walks both contexts, queues OutboxMessage rows into appDb
          -> EF INSERT into testplan_db.task_run
        await appDb.SaveChangesAsync(ct)
          -> EF INSERT into app_db.outbox_message
    SharedConnection.CommitAsync()  // both INSERTs commit atomically
```

Same MySQL server -> one `MySqlConnection` opens once per request, both DbContexts pull from it,
one `MySqlTransaction` wraps everything. Cross-DB writes commit (or roll back) as one unit.

Not XA / distributed transactions - those are for cross-server cases.

Wiring:
- `SharedConnection` (`Infrastructure/Persistence/`) - scoped per request, lazy-opens
- `UnitOfWorkBehavior` (`Application/Behaviors/`) - calls `IUnitOfWork.BeginAsync` for commands
- `UnitOfWork` impl (`Infrastructure/Dispatching/`) - calls `Database.UseTransactionAsync` on
  both DbContexts so EF participates
- Both DbContexts registered in DI with a factory that pulls the connection from `SharedConnection`

## Outbox

Publishing to RabbitMQ separately from the business write is unsafe: if the broker is down or the
process crashes between the write and the publish, the event is lost. The outbox makes the event
durably alongside the write.

```
Aggregate raises IDomainEvent
        |
        v
SaveChangesAsync                  OutboxPublisher (BackgroundService)
   OutboxInterceptor                       |
   serialises event                        | poll every 1s
   inserts OutboxMessage row               v
        |                          SELECT WHERE processed_at IS NULL
        v                                  |
   Same tx as the business write           v
        |                          channel.BasicPublish(...)
        v                          channel.WaitForConfirmsOrDie(5s)
   COMMIT  ---> durable row  <---  UPDATE outbox SET processed_at = NOW()
                  |
                  v
            RabbitMQ topic exchange "testers.events.v1"
                  |
                  v
            Routing key e.g. "task.run.recorded.v1"
                  |
                  v
            Consumers (read-model projectors, notifications, etc.)
```

What you get:

| Failure | Result |
|---|---|
| Crash before SaveChanges | Nothing committed -> nothing lost. |
| Crash after SaveChanges but before publish | Row durable + unprocessed -> next poll publishes. |
| RabbitMQ down | Polls fail, row stays unprocessed -> recovers when broker is back. |
| Broker accepts publish then dies before persisting | `WaitForConfirmsOrDie` throws -> retry next tick. |
| Consumer processes twice | Idempotency via `MessageId = OutboxMessage.Id` + inbox dedup. |

Routing key: `{aggregate}.{event}.v{N}`, lowercased, derived from the event class name with
Event/DomainEvent suffix stripped. `TaskRunRecorded` -> `task.run.recorded.v1`. Versioning in the
key lets v2 consumers coexist with v1.

Single-instance publisher for now. For scale-out, change the SELECT to `... FOR UPDATE SKIP LOCKED`
(MySQL 8+) so multiple instances claim disjoint slices.

Inbox pattern for incoming events from other apps: consumer keeps a `(MessageId, ConsumerName)`
row in `app_db.inbox_message`, checks before processing, inserts in the same tx as its business
write. Gives exactly-once effect on top of at-least-once delivery. ConsumerHost wiring deferred
until a concrete incoming event arrives.

## Audit + soft-delete

Two interfaces, one interceptor:

- `IAuditable` - `CreatedAt`/`CreatedBy`/`ModifiedAt`/`ModifiedBy`. Stamped by `AuditInterceptor`
  on SaveChanges from `ICurrentUser` + `IClock`. `AggregateRoot<TId>` implements this.
- `ISoftDeletable` - `IsDeleted`/`DeletedAt`/`DeletedBy`. Interceptor converts `EntityState.Deleted`
  to `Modified` and flips the flags.

Mixed policy: soft-delete user-facing entities (TaskRun, TestPlan, Comment); hard-delete transient
ones. AuditLog row writes are deferred (entity + stamping logic in place; cross-DB AuditLog write
path comes with the first audited delete that matters).

## Auth

| Scheme | For | How |
|---|---|---|
| `AuthSchemes.OktaJwt` | Humans | `AddJwtBearer` validates against Okta JWKS, populates ClaimsPrincipal. |
| `AuthSchemes.ApiKey` | Services | Custom AuthenticationHandler reads `X-Api-Key`, SHA-256, looks up `app_db.api_key` (unique index = sub-ms). |

Both project into the same `ICurrentUser`:

```
HttpContext.User -- projected by HttpContextCurrentUser --> ICurrentUser
  sub claim          -> .Id            (e.g. "okta:user-42" or "apikey:{guid}")
  name claim         -> .DisplayName
  user:kind claim    -> .Kind          (Human | Service)
  role claims        -> .Roles         (Okta groups OR API-key scopes)
```

Handlers and interceptors consume `ICurrentUser`. They don't branch on Okta-vs-ApiKey; the auth
handler is the one place that knows.

Policies in `AuthorizationPolicies.cs`:
- `AnyAuthenticated` - default; either scheme accepted
- `HumanOnly` - endpoints needing a real user (e.g. submit a bug)
- `ServiceOnly` - admin/bulk endpoints for CI

## Dispatcher pipeline

Hand-rolled (MediatR went commercial from v12). ~150 lines total.

```
IDispatcher.Send<TResponse>(IRequest<TResponse>)
         |
         v
LoggingBehavior         one structured log per request
         |
         v
ValidationBehavior      runs FluentValidation; throws ValidationException
         |
         v
UnitOfWorkBehavior      commands only - begins shared transaction
         |
         v
PerformanceBehavior     warns if handler exceeds 500ms
         |
         v
IRequestHandler<TRequest, TResponse>.Handle(...)
```

Behaviors registered open-generic; dispatcher uses reflection to compose them per request.
Reflection cost is microseconds vs ms of DB I/O. Switch to compiled-expression caches if
profiling ever shows it.

UnitOfWorkBehavior opens a transaction only for `ICommand<_>` / `ICommand` requests. Queries skip.

## Cache

Two patterns side by side:

**Generic ICache (Redis):**
- `Application/Abstractions/ICache.cs` -> `Infrastructure/Cache/RedisCache.cs`
- `Get`/`Set`/`Remove`/`GetOrAddAsync<T>(key, ...)`
- Cross-instance, ad-hoc, string-keyed
- `GetOrAddAsync` does NOT coalesce concurrent misses on the same key

**Typed DataCache<TKey, TValue> (in-memory, vendored):**
- `Infrastructure/Cache/Library/` (vendored from github.com/ncowine/CacheRepository)
- Subclass per use-case; implement `FetchAsync` (single + batch)
- Concurrent-fetch coalescing - N parallel `Get(sameKey)` triggers ONE FetchAsync
- In-memory only; single-instance

See `Cache/Library/NOTICE.md` for vendoring details.

## Observability

- Serilog as host logger. Console + rolling file (`logs/testers-api-{date}.log`).
- `CorrelationIdMiddleware` reads/sets `X-Correlation-Id`, pushes to `Serilog.LogContext`. Every
  log line in the request scope is enriched, including the outbox row's CorrelationId so it
  flows out on the AMQP message.
- Enrichers: MachineName, ProcessId, ThreadId, Application, Environment.
- `SerilogRequestLogging` middleware emits one structured log per HTTP request.

OpenTelemetry tracing not wired yet. Add when there's a real backend to push to.

## Adding a feature slice

Example: `CompleteTestPlan` in the Catalog area.

```
src/Testers.Application/Features/Catalog/CompleteTestPlan/
  CompleteTestPlanRequest.cs    HTTP body DTO
  CompleteTestPlanCommand.cs    record : ICommand<CompleteTestPlanResult>
  CompleteTestPlanResult.cs     response DTO
  CompleteTestPlanValidator.cs  AbstractValidator<CompleteTestPlanCommand>
  CompleteTestPlanHandler.cs    internal sealed : IRequestHandler<..., ...>
  CompleteTestPlanEndpoint.cs   : IEndpoint
```

`AddApplication()` scans for the handler + validator. `EndpointScanner` maps the endpoint under
`/api/v1`. Domain types go in `src/Testers.Domain/Catalog/`. EF mapping goes in the right
DbContext's `OnModelCreating`.

`Features/Execution/ActionTaskRun/` is the reference example.

## Folder rule

A folder exists to group 2+ siblings of the same category. A singleton sits at its parent level
without a wrapping folder.

Applied to projects (no `Host/` wrapping a single Api project) and inside projects (`Abstractions/`,
`Behaviors/`, `Features/`, `Persistence/`, `Messaging/`, `Cache/`, `Auth/` - but `Program.cs`,
`DependencyInjection.cs`, `SystemClock.cs` at project root).

Folder paths become namespace paths by default.

## Decisions log

| Considered | Rejected because |
|---|---|
| Modular Monolith (per-context project bundles) | ~20 projects for a one-dev POC. Used layered + feature folders. |
| MediatR | Commercial licence from v12. Hand-rolled IDispatcher (~150 lines). |
| FluentAssertions v8+ | Commercial licence. Used Shouldly (MIT). |
| Moq | License/funding drama. Used NSubstitute (BSD). |
| MassTransit | Commercial in v9. Used raw RabbitMQ.Client + a thin wrapper. |
| Repository pattern | DbContext is already a repository. Handlers inject `IAppDbContext` / `ITestPlanDbContext`. |
| Cross-DB JOINs | Couples us to the other team's schema. API composition + caching instead. |
| Generic `Repository<T>` | Speculative. Add when 3+ slices want the same thing. |
| In-process domain-event dispatch | Hard to make atomic with the business write. Outbox-via-interceptor is bulletproof. |
| `IEventPublisher` abstraction | No callers (OutboxPublisher works with serialised OutboxMessage rows). Removed. |
| BCrypt for API keys | Keys are 256 bits of entropy. SHA-256 is sufficient + fast. |
| ConsumerHost without consumers | Skeleton-without-purpose. Add when the first concrete incoming event arrives. |
| `FOR UPDATE SKIP LOCKED` outbox | Not needed at single instance. Add when scaling out. |
| OpenTelemetry tracing | No backend to push to yet. |
| Strict Onion (Domain owns interfaces to DbContext) | We do this for `IAppDbContext` / `ITestPlanDbContext`. Application references EF Core directly via `Microsoft.AspNetCore.App` framework ref. |

## Where to look

| Question | File |
|---|---|
| How is a handler dispatched? | `Infrastructure/Dispatching/Dispatcher.cs` |
| How are domain events captured? | `Infrastructure/Outbox/OutboxInterceptor.cs` |
| How are events published? | `Infrastructure/Messaging/OutboxPublisher.cs` |
| How are audit fields stamped? | `Infrastructure/Audit/AuditInterceptor.cs` |
| How does the cross-DB transaction work? | `Infrastructure/Persistence/SharedConnection.cs` + `Dispatching/UnitOfWork.cs` |
| How is `ICurrentUser` populated from HTTP? | `Api/Auth/HttpContextCurrentUser.cs` |
| How does API-key auth work? | `Api/Auth/ApiKeyAuthenticationHandler.cs` |
| How does composition work? | `Api/Program.cs` |
| Reference feature slice | `Application/Features/Execution/ActionTaskRun/` |
| Reference aggregate | `Domain/Execution/TaskRun.cs` |
| Reference unit tests | `tests/Testers.UnitTests/` |
