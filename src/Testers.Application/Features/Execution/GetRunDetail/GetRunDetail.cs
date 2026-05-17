using FluentValidation;
using Testers.Application.Abstractions;
using Testers.Contracts.Execution;

namespace Testers.Application.Features.Execution.GetRunDetail;

public sealed record GetRunDetailQuery(Guid TaskRunId) : IQuery<RunDetail>;

public sealed class GetRunDetailValidator : AbstractValidator<GetRunDetailQuery>
{
    public GetRunDetailValidator()
    {
        RuleFor(q => q.TaskRunId).NotEmpty();
    }
}
