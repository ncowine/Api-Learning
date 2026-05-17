namespace Testers.Contracts.Execution;

// POST Routes.Runs.BugLinks(taskRunId)  - route supplies taskRunId.

public sealed record LinkBugRequest(string ExternalBugId, string? BugTrackerUrl);

public sealed record LinkBugResult(Guid BugLinkId, DateTime LinkedAt);
