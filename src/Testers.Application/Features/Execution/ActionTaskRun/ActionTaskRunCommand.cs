using Testers.Application.Abstractions;

namespace Testers.Application.Features.Execution.ActionTaskRun;

// Validator rejects unknown outcomes (PASS/FAIL/SKIP/BLOCK) before this hits the handler.
public sealed record ActionTaskRunCommand(
    Guid TaskDefinitionId,
    Guid BuildId,
    string Outcome,
    string? Note) : ICommand<ActionTaskRunResult>;
