using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testers.SharedKernel.Abstractions;
using Testers.Domain.Abstractions;

namespace Testers.Infrastructure.Audit;

// Stamps audit fields on IAuditable; converts deletes of ISoftDeletable into soft-deletes.
// Attached to both DbContexts. AuditLog row writes deferred.
internal sealed class AuditInterceptor(IClock clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null) Stamp(eventData.Context.ChangeTracker.Entries());
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null) Stamp(eventData.Context.ChangeTracker.Entries());
        return base.SavingChanges(eventData, result);
    }

    private void Stamp(IEnumerable<EntityEntry> entries)
    {
        var now = clock.UtcNow;
        var who = currentUser.IsAuthenticated ? currentUser.Id : "system";

        foreach (var entry in entries)
        {
            // Soft-delete: demote to Modified + flip flags so EF UPDATEs instead of DELETEs.
            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable })
            {
                entry.State = EntityState.Modified;
                entry.CurrentValues["IsDeleted"] = true;
                entry.CurrentValues["DeletedAt"] = now;
                entry.CurrentValues["DeletedBy"] = who;
                continue;
            }

            if (entry.Entity is not IAuditable) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.CurrentValues[nameof(IAuditable.CreatedAt)] = now;
                    entry.CurrentValues[nameof(IAuditable.CreatedBy)] = who;
                    break;

                case EntityState.Modified:
                    entry.CurrentValues[nameof(IAuditable.ModifiedAt)] = now;
                    entry.CurrentValues[nameof(IAuditable.ModifiedBy)] = who;
                    break;
            }
        }
    }
}
