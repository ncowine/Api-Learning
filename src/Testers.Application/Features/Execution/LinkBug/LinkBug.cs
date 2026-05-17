using FluentValidation;
using Testers.Application.Abstractions;
using Testers.Contracts.Execution;

namespace Testers.Application.Features.Execution.LinkBug;

public sealed record LinkBugCommand(Guid TaskRunId, string ExternalBugId, string? BugTrackerUrl) : ICommand<LinkBugResult>;

public sealed class LinkBugValidator : AbstractValidator<LinkBugCommand>
{
    public LinkBugValidator()
    {
        RuleFor(c => c.TaskRunId).NotEmpty();
        RuleFor(c => c.ExternalBugId).NotEmpty().MaximumLength(200);
        RuleFor(c => c.BugTrackerUrl).MaximumLength(1000);
    }
}
