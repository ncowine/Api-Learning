namespace Testers.Domain.Abstractions;

/// <summary>
/// Contract for entities whose Created/Modified audit fields are stamped automatically by the
/// AuditInterceptor (Infrastructure) at <c>SaveChangesAsync</c> time. Handlers never write
/// these manually — they're populated from <c>ICurrentUser</c> and <c>IClock</c>.
///
/// Setters are intentionally private on implementations; EF Core's reflection-based property
/// access lets the interceptor write them, but domain code cannot.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; }

    string CreatedBy { get; }

    DateTime? ModifiedAt { get; }

    string? ModifiedBy { get; }
}
