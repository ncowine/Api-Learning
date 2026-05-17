using FluentValidation;
using Testers.Domain.Execution;

namespace Testers.Application.Features.Execution.ActionTaskRun;

/// <summary>
/// Auto-registered by <c>AddApplication()</c> via <c>AddValidatorsFromAssemblyContaining</c>.
/// <c>ValidationBehavior</c> runs this before the handler; failures throw
/// <c>ValidationException</c> → 400 ProblemDetails by the Api project's ExceptionHandler.
/// </summary>
public sealed class ActionTaskRunValidator : AbstractValidator<ActionTaskRunCommand>
{
    public ActionTaskRunValidator()
    {
        RuleFor(c => c.TaskDefinitionId)
            .NotEmpty().WithMessage("TaskDefinitionId is required.");

        RuleFor(c => c.BuildId)
            .NotEmpty().WithMessage("BuildId is required.");

        RuleFor(c => c.Outcome)
            .NotEmpty().WithMessage("Outcome is required.")
            .Must(BeKnownOutcome)
            .WithMessage($"Outcome must be one of: {string.Join(", ", TaskOutcome.All.Select(o => o.Name))}.");

        RuleFor(c => c.Note)
            .MaximumLength(2000).WithMessage("Note cannot exceed 2000 characters.");
    }

    private static bool BeKnownOutcome(string outcome) =>
        !string.IsNullOrWhiteSpace(outcome)
        && TaskOutcome.All.Any(o => string.Equals(o.Name, outcome, StringComparison.OrdinalIgnoreCase));
}
