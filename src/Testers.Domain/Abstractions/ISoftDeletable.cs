namespace Testers.Domain.Abstractions;

// Marker. AuditInterceptor converts Deleted -> Modified and flips these.
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    string? DeletedBy { get; }
}
