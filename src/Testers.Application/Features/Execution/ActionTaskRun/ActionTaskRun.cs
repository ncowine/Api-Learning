using FluentValidation;
using Testers.Application.Abstractions;
using Testers.Domain.Execution;

namespace Testers.Application.Features.Execution.ActionTaskRun;

// The shape of the slice: command, response, validator. Handler lives next door.

public sealed record ActionTaskRunCommand(
    Guid TaskDefinitionId,
    Guid BuildId,
    string Outcome,
    string? Note) : ICommand<ActionTaskRunResult>;

public sealed record ActionTaskRunResult(
    Guid TaskRunId,
    string Outcome,
    DateTime ActionedAt);

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
