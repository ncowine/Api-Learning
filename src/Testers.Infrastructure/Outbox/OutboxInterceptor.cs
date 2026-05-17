using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Testers.SharedKernel.Abstractions;
using Testers.Domain.Abstractions;
using Testers.Infrastructure.Persistence;

namespace Testers.Infrastructure.Outbox;

// Walks both DbContexts on each SaveChanges, serialises domain events from aggregates, queues
// OutboxMessage rows into AppDb. Lazy IServiceProvider injection avoids the DI cycle (DbContext
// registers this; this needs DbContexts).
//
// When firing on TestPlanDbContext, rows go into AppDb's tracker but won't commit until the
// caller also saves AppDb. Handlers writing cross-DB end with both SaveChangesAsync calls.
internal sealed class OutboxInterceptor(IServiceProvider services, IClock clock) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null) EmitOutboxRows(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null) EmitOutboxRows(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    // clock kept on the ctor for future use (replay scenarios overriding OccurredAt).
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "reserved")]
    private void EmitOutboxRows(DbContext savingContext)
    {
        _ = clock;
        var rows = new List<OutboxMessage>();

        foreach (var ctx in ScopedContexts(savingContext))
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

        if (rows.Count == 0) return;
        services.GetRequiredService<AppDbContext>().Outbox.AddRange(rows);
    }

    private IEnumerable<DbContext> ScopedContexts(DbContext savingContext)
    {
        yield return savingContext;

        var appDb = services.GetService<AppDbContext>();
        if (appDb is not null && !ReferenceEquals(appDb, savingContext)) yield return appDb;

        var testPlanDb = services.GetService<TestPlanDbContext>();
        if (testPlanDb is not null && !ReferenceEquals(testPlanDb, savingContext)) yield return testPlanDb;
    }

    // TaskRunRecorded -> "task.run.recorded.v1". Strips Event/DomainEvent suffix.
    private static string BuildRoutingKey(IDomainEvent evt)
    {
        var typeName = evt.GetType().Name;
        var stripped = typeName.EndsWith("DomainEvent", StringComparison.Ordinal) ? typeName[..^"DomainEvent".Length]
                     : typeName.EndsWith("Event", StringComparison.Ordinal) ? typeName[..^"Event".Length]
                     : typeName;

        var sb = new StringBuilder(stripped.Length + 5);
        for (var i = 0; i < stripped.Length; i++)
        {
            if (i > 0 && char.IsUpper(stripped[i])) sb.Append('.');
            sb.Append(char.ToLowerInvariant(stripped[i]));
        }
        sb.Append(".v1");
        return sb.ToString();
    }
}
