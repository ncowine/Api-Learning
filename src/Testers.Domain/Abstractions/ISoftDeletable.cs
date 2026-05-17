namespace Testers.Domain.Abstractions;

/// <summary>
/// Contract for entities that should be soft-deleted: the AuditInterceptor converts
/// <c>EntityState.Deleted</c> into <c>EntityState.Modified</c>, sets <see cref="IsDeleted"/>
/// + <see cref="DeletedAt"/> + <see cref="DeletedBy"/>, and a global query filter on the
/// DbContext hides the row from normal queries.
///
/// Entities NOT implementing this are hard-deleted (the row is physically removed). For both
/// kinds, an AuditLog row records who performed the deletion.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }

    DateTime? DeletedAt { get; }

    string? DeletedBy { get; }
}
