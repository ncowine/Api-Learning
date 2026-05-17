using Testers.Domain.Abstractions;

namespace Testers.Domain.Execution.Events;

public sealed record CommentAddedToRun(
    Guid EventId,
    DateTime OccurredAt,
    Guid TaskRunId,
    Guid CommentId,
    string AuthorId,
    string Body) : IDomainEvent;
