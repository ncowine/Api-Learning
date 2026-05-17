using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Testers.Application.Abstractions;
using Testers.Domain.Abstractions;
using Testers.Infrastructure.Persistence;

namespace Testers.Infrastructure.Outbox;

/// <summary>
/// EF Core <see cref="SaveChangesInterceptor"/> that captures <see cref="IDomainEvent"/>s off any
/// tracked <see cref="IAggregateRoot"/> in EITHER DbContext, serialises them, and adds
/// <see cref="OutboxMessage"/> rows to <see cref="AppDbContext"/>. Events are cleared from the
/// aggregate after queuing so re-saves don't duplicate.
///
/// Lifecycle: scoped. Lazy-resolves DbContexts via <see cref="IServiceProvider"/> to avoid the
/// DI cycle (DbContext registers this interceptor, this interceptor needs DbContexts).
///
/// IMPORTANT: when this interceptor fires for <c>TestPlanDbContext.SaveChangesAsync</c>, the
/// OutboxMessage rows are *queued* into <c>AppDbContext.ChangeTracker</c> but not committed.
/// The handler / <c>UnitOfWorkBehavior</c> must call <c>appDb.SaveChangesAsync</c> within the
/// same shared transaction for the outbox rows to land. (In practice handlers that write
/// cross-DB end with both <c>SaveChangesAsync</c> calls before the UoW commits.)
/// </summary>
internal sealed class OutboxInterceptor(IServiceProvider services, IClock clock) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            EmitOutboxRows(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            EmitOutboxRows(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    // 'clock' is part of the constructor in case we later want to override OccurredAt for tests;
    // currently we trust the event's own OccurredAt (set by the aggregate when raising).
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060",
        Justification = "Reserved for future use (override OccurredAt for replay scenarios).")]
    private void EmitOutboxRows(DbContext savingContext)
    {
        _ = clock; // suppress 'field never used' until clock-driven OccurredAt override is needed
        var contextsToScan = GetAllScopedContexts(savingContext).ToArray();
        var rows = new List<OutboxMessage>();

        foreach (var ctx in contextsToScan)
        {
            var roots = ctx.ChangeTracker.Entries()
                .Where(e => e.Entity is IAggregateRoot root && root.DomainEvents.Count > 0)
                .Select(e => (IAggregateRoot)e.Entity)
                .ToArray();

            foreach (var root in roots)
            {
                foreach (var evt in root.DomainEvents)
                {
                    rows.Add(new OutboxMessage(
                        evt.EventId,
                        evt.GetType().FullName ?? evt.GetType().Name,
                        BuildRoutingKey(evt),
                        JsonSerializer.Serialize(evt, evt.GetType(), JsonOptions),
                        evt.OccurredAt,
                        correlationId: null));
                }

                root.ClearDomainEvents();
            }
        }

        if (rows.Count == 0)
        {
            return;
        }

        var appDb = services.GetRequiredService<AppDbContext>();
        appDb.Outbox.AddRange(rows);
    }

    /// <summary>Yields both DbContexts in scope; the saving context first to avoid double-resolution.</summary>
    private IEnumerable<DbContext> GetAllScopedContexts(DbContext savingContext)
    {
        yield return savingContext;

        var appDb = services.GetService<AppDbContext>();
        if (appDb is not null && !ReferenceEquals(appDb, savingContext))
        {
            yield return appDb;
        }

        var testPlanDb = services.GetService<TestPlanDbContext>();
        if (testPlanDb is not null && !ReferenceEquals(testPlanDb, savingContext))
        {
            yield return testPlanDb;
        }
    }

    /// <summary>
    /// Convention: <c>{aggregate}.{event}.v1</c> derived from the event class name, lowercased,
    /// with "Event" / "DomainEvent" suffix stripped. <c>TaskRunRecorded</c> → <c>task.run.recorded.v1</c>.
    /// Future enhancement: read a <c>[EventVersion(N)]</c> attribute to bump the .vN suffix.
    /// </summary>
    private static string BuildRoutingKey(IDomainEvent evt)
    {
        var typeName = evt.GetType().Name;
        var stripped = typeName.EndsWith("DomainEvent", StringComparison.Ordinal)
            ? typeName[..^"DomainEvent".Length]
            : typeName.EndsWith("Event", StringComparison.Ordinal)
                ? typeName[..^"Event".Length]
                : typeName;

        var sb = new StringBuilder(stripped.Length + 5);
        for (var i = 0; i < stripped.Length; i++)
        {
            if (i > 0 && char.IsUpper(stripped[i]))
            {
                sb.Append('.');
            }

            sb.Append(char.ToLowerInvariant(stripped[i]));
        }

        sb.Append(".v1");
        return sb.ToString();
    }
}
