using Testers.Application.Abstractions;

namespace Testers.Application.Features.Execution.ActionTaskRun;

/// <summary>
/// Command dispatched through <see cref="IDispatcher"/>. Combines the route's
/// <c>taskDefinitionId</c> with the body fields. <see cref="Outcome"/> is the smart-enum
/// name (PASS/FAIL/SKIP/BLOCK); validation rejects unknown values before the handler runs.
/// </summary>
public sealed record ActionTaskRunCommand(
    Guid TaskDefinitionId,
    Guid BuildId,
    string Outcome,
    string? Note)
    : ICommand<ActionTaskRunResult>;
