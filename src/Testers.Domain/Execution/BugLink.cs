using Testers.Domain.Abstractions;

namespace Testers.Domain.Execution;

/// <summary>
/// A link from a TaskRun to an external bug-tracker ticket (Jira, GitHub Issues, etc.).
/// Owned by <see cref="TaskRun"/>; we don't model the bug itself, only the reference.
/// </summary>
public sealed class BugLink : Entity<Guid>
{
    /// <summary>The bug tracker's own id (e.g. <c>"PROJ-1234"</c>).</summary>
    public string ExternalBugId { get; private set; } = string.Empty;

    /// <summary>Optional deep-link URL for one-click navigation from the UI.</summary>
    public string? BugTrackerUrl { get; private set; }

    public DateTime LinkedAt { get; private set; }

    private BugLink()
    {
        // EF Core materialisation only.
    }

    internal BugLink(string externalBugId, string? bugTrackerUrl, DateTime linkedAt) : base(Guid.NewGuid())
    {
        ExternalBugId = externalBugId;
        BugTrackerUrl = bugTrackerUrl;
        LinkedAt = linkedAt;
    }
}
