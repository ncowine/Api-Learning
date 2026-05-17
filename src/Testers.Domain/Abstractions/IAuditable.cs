namespace Testers.Domain.Abstractions;

// Stamped by AuditInterceptor on SaveChanges. Setters private on the entity;
// EF writes them via reflection.
public interface IAuditable
{
    DateTime CreatedAt { get; }
    string CreatedBy { get; }
    DateTime? ModifiedAt { get; }
    string? ModifiedBy { get; }
}
