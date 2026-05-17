using Testers.Domain.Abstractions;

namespace Testers.Domain.Execution.Events;

public sealed record TaskRunRecorded(
    Guid EventId,
    DateTime OccurredAt,
    Guid TaskRunId,
    Guid TaskDefinitionId,
    Guid BuildId,
    string TesterId,
    string Outcome,
    string? Note) : IDomainEvent;
