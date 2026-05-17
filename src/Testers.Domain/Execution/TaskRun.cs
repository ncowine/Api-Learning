using Testers.Domain.Abstractions;
using Testers.Domain.Execution.Events;

namespace Testers.Domain.Execution;

/// <summary>
/// The execution-side aggregate root: "tester X actioned task Y on build Z with outcome O".
///
/// Lives in the shared TestPlan DB (every consuming app sees TaskRuns). The constructor enforces
/// invariants and raises <see cref="TaskRunRecorded"/>; the OutboxInterceptor captures the event
/// at <c>SaveChangesAsync</c> time and queues an OutboxMessage into the App DB in the same
/// transaction.
///
/// Owned entities (<see cref="Comments"/>, <see cref="BugLinks"/>) live and die with the run.
/// </summary>
public sealed class TaskRun : AggregateRoot<Guid>
{
    private readonly List<Comment> _comments = new();
    private readonly List<BugLink> _bugLinks = new();

    public Guid TaskDefinitionId { get; private set; }

    public Guid BuildId { get; private set; }

    public string TesterId { get; private set; } = string.Empty;

    public TaskOutcome Outcome { get; private set; } = TaskOutcome.Skip;

    public string? Note { get; private set; }

    public DateTime ActionedAt { get; private set; }

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    public IReadOnlyCollection<BugLink> BugLinks => _bugLinks.AsReadOnly();

    private TaskRun()
    {
        // EF Core materialisation only.
    }

    public TaskRun(
        Guid taskDefinitionId,
        Guid buildId,
        string testerId,
        TaskOutcome outcome,
        string? note,
        DateTime actionedAt)
        : base(Guid.NewGuid())
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(testerId);
        if (taskDefinitionId == Guid.Empty)
        {
            throw new ArgumentException("TaskDefinitionId must be a non-empty Guid.", nameof(taskDefinitionId));
        }

        if (buildId == Guid.Empty)
        {
            throw new ArgumentException("BuildId must be a non-empty Guid.", nameof(buildId));
        }

        TaskDefinitionId = taskDefinitionId;
        BuildId = buildId;
        TesterId = testerId;
        Outcome = outcome;
        Note = note;
        ActionedAt = actionedAt;

        Raise(new TaskRunRecorded(
            EventId: Guid.NewGuid(),
            OccurredAt: actionedAt,
            TaskRunId: Id,
            TaskDefinitionId: taskDefinitionId,
            BuildId: buildId,
            TesterId: testerId,
            Outcome: outcome.Name,
            Note: note));
    }

    /// <summary>Append a comment to this run. The comment lives within the aggregate.</summary>
    public Comment AddComment(string body, string authorId, DateTime addedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(authorId);

        var comment = new Comment(body, authorId, addedAt);
        _comments.Add(comment);
        return comment;
    }

    /// <summary>Link an external bug to this run.</summary>
    public BugLink LinkBug(string externalBugId, string? bugTrackerUrl, DateTime linkedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalBugId);

        var link = new BugLink(externalBugId, bugTrackerUrl, linkedAt);
        _bugLinks.Add(link);
        return link;
    }
}
