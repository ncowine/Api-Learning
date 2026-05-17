using FluentValidation;
using Testers.Application.Abstractions;
using Testers.Contracts.Execution;

namespace Testers.Application.Features.Execution.ListRunsForBuild;

public sealed record ListRunsForBuildQuery(Guid BuildId) : IQuery<IReadOnlyList<RunSummary>>;

public sealed class ListRunsForBuildValidator : AbstractValidator<ListRunsForBuildQuery>
{
    public ListRunsForBuildValidator()
    {
        RuleFor(q => q.BuildId).NotEmpty();
    }
}
