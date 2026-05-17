namespace Testers.Contracts.Execution;

// Compact row used by the list endpoints (by task or by build). The detail endpoint returns
// a richer shape (RunDetail) with comments + bug links.
public sealed record RunSummary(
    Guid TaskRunId,
    Guid TaskDefinitionId,
    Guid BuildId,
    string TesterId,
    string Outcome,
    DateTime ActionedAt);
