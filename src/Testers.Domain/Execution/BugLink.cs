using Testers.Domain.Abstractions;

namespace Testers.Domain.Execution;

// Owned by TaskRun. We store the reference, not the bug itself.
public sealed class BugLink : Entity<Guid>
{
    public string ExternalBugId { get; private set; } = string.Empty;
    public string? BugTrackerUrl { get; private set; }
    public DateTime LinkedAt { get; private set; }

    private BugLink() { }  // EF

    internal BugLink(string externalBugId, string? bugTrackerUrl, DateTime linkedAt) : base(Guid.NewGuid())
    {
        ExternalBugId = externalBugId;
        BugTrackerUrl = bugTrackerUrl;
        LinkedAt = linkedAt;
    }
}
