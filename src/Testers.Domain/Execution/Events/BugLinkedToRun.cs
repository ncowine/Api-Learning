using Testers.Domain.Abstractions;

namespace Testers.Domain.Execution.Events;

public sealed record BugLinkedToRun(
    Guid EventId,
    DateTime OccurredAt,
    Guid TaskRunId,
    Guid BugLinkId,
    string ExternalBugId,
    string? BugTrackerUrl) : IDomainEvent;
