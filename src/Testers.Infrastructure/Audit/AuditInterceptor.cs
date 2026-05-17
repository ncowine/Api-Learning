using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testers.Application.Abstractions;
using Testers.Domain.Abstractions;

namespace Testers.Infrastructure.Audit;

/// <summary>
/// EF Core <see cref="SaveChangesInterceptor"/> that:
/// <list type="bullet">
///   <item>Stamps <c>CreatedAt</c> / <c>CreatedBy</c> on Added <see cref="IAuditable"/> entities.</item>
///   <item>Stamps <c>ModifiedAt</c> / <c>ModifiedBy</c> on Modified <see cref="IAuditable"/> entities.</item>
///   <item>Converts <c>EntityState.Deleted</c> to <c>Modified</c> on <see cref="ISoftDeletable"/> entities
///         and sets <c>IsDeleted</c> / <c>DeletedAt</c> / <c>DeletedBy</c>.</item>
/// </list>
/// Attached to both DbContexts. Runs inside <c>SaveChangesAsync</c> so its writes commit in the
/// same transaction as the business changes.
///
/// AuditLog row writes are deferred — they need cross-DB plumbing fully wired through and will
/// land in a follow-up sub-step.
/// </summary>
internal sealed class AuditInterceptor(IClock clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            Stamp(eventData.Context.ChangeTracker.Entries());
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            Stamp(eventData.Context.ChangeTracker.Entries());
        }

        return base.SavingChanges(eventData, result);
    }

    private void Stamp(IEnumerable<EntityEntry> entries)
    {
        var now = clock.UtcNow;
        var who = currentUser.IsAuthenticated ? currentUser.Id : "system";

        foreach (var entry in entries)
        {
            // Soft-delete: flip the flags and demote to Modified so the row updates instead of deleting.
            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable })
            {
                entry.State = EntityState.Modified;
                entry.CurrentValues["IsDeleted"] = true;
                entry.CurrentValues["DeletedAt"] = now;
                entry.CurrentValues["DeletedBy"] = who;
                continue;
            }

            if (entry.Entity is not IAuditable)
            {
                continue;
            }

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

                default:
                    break;
            }
        }
    }
}
