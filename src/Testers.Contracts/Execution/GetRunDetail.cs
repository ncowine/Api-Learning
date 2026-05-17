namespace Testers.Contracts.Execution;

// GET Routes.Runs.Detail(taskRunId)

public sealed record RunDetail(
    Guid TaskRunId,
    Guid TaskDefinitionId,
    Guid BuildId,
    string TesterId,
    string Outcome,
    string? Note,
    DateTime ActionedAt,
    IReadOnlyList<CommentDto> Comments,
    IReadOnlyList<BugLinkDto> BugLinks);

public sealed record CommentDto(Guid Id, string Body, string AuthorId, DateTime AddedAt);

public sealed record BugLinkDto(Guid Id, string ExternalBugId, string? BugTrackerUrl, DateTime LinkedAt);
