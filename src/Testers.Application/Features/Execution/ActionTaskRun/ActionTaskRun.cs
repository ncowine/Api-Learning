using FluentValidation;
using Testers.Application.Abstractions;
using Testers.Contracts.Execution;
using Testers.Domain.Execution;

namespace Testers.Application.Features.Execution.ActionTaskRun;

// Server-internal Command (carries ICommand<>). The wire shape (Request body, Result response)
// lives in Testers.Contracts so clients can reference one tiny project.
public sealed record ActionTaskRunCommand(
    Guid TaskDefinitionId,
    Guid BuildId,
    string Outcome,
    string? Note) : ICommand<ActionTaskRunResult>;

public sealed class ActionTaskRunValidator : AbstractValidator<ActionTaskRunCommand>
{
    public ActionTaskRunValidator()
    {
        RuleFor(c => c.TaskDefinitionId).NotEmpty();
        RuleFor(c => c.BuildId).NotEmpty();
        RuleFor(c => c.Outcome)
            .NotEmpty()
            .Must(IsKnown)
            .WithMessage($"Outcome must be one of: {string.Join(", ", TaskOutcome.All.Select(o => o.Name))}.");
        RuleFor(c => c.Note).MaximumLength(2000);
    }

    private static bool IsKnown(string outcome) =>
        !string.IsNullOrWhiteSpace(outcome)
        && TaskOutcome.All.Any(o => string.Equals(o.Name, outcome, StringComparison.OrdinalIgnoreCase));
}
