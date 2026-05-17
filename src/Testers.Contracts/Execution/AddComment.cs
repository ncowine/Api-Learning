namespace Testers.Contracts.Execution;

// POST Routes.Runs.Comments(taskRunId)  - route supplies taskRunId, body supplies the text.

public sealed record AddCommentRequest(string Body);

public sealed record AddCommentResult(Guid CommentId, DateTime AddedAt);
