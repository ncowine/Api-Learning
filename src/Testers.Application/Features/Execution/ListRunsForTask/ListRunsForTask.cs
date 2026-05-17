using FluentValidation;
using Testers.SharedKernel.Abstractions;
using Testers.Contracts.Execution;

namespace Testers.Application.Features.Execution.ListRunsForTask;

public sealed record ListRunsForTaskQuery(Guid TaskDefinitionId) : IQuery<IReadOnlyList<RunSummary>>;

public sealed class ListRunsForTaskValidator : AbstractValidator<ListRunsForTaskQuery>
{
    public ListRunsForTaskValidator()
    {
        RuleFor(q => q.TaskDefinitionId).NotEmpty();
    }
}
