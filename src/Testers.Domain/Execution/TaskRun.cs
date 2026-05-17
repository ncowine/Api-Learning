using Testers.Domain.Abstractions;
using Testers.Domain.Execution.Events;

namespace Testers.Domain.Execution;

// Lives in the shared TestPlan DB so other apps see it. Ctor raises TaskRunRecorded;
// OutboxInterceptor picks it up on SaveChanges and queues an outbox row into AppDb.
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

    private TaskRun() { }  // EF

    public TaskRun(
        Guid taskDefinitionId,
        Guid buildId,
        string testerId,
        TaskOutcome outcome,
        string? note,
        DateTime actionedAt) : base(Guid.NewGuid())
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(testerId);
        if (taskDefinitionId == Guid.Empty) throw new ArgumentException("Empty.", nameof(taskDefinitionId));
        if (buildId == Guid.Empty) throw new ArgumentException("Empty.", nameof(buildId));

        TaskDefinitionId = taskDefinitionId;
        BuildId = buildId;
        TesterId = testerId;
        Outcome = outcome;
        Note = note;
        ActionedAt = actionedAt;

        Raise(new TaskRunRecorded(
            Guid.NewGuid(), actionedAt, Id,
            taskDefinitionId, buildId, testerId, outcome.Name, note));
    }

    public Comment AddComment(string body, string authorId, DateTime addedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(authorId);
        var c = new Comment(body, authorId, addedAt);
        _comments.Add(c);
        return c;
    }

    public BugLink LinkBug(string externalBugId, string? bugTrackerUrl, DateTime linkedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalBugId);
        var link = new BugLink(externalBugId, bugTrackerUrl, linkedAt);
        _bugLinks.Add(link);
        return link;
    }
}
