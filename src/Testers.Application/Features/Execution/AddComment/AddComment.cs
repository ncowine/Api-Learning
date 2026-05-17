using FluentValidation;
using Testers.SharedKernel.Abstractions;
using Testers.Contracts.Execution;

namespace Testers.Application.Features.Execution.AddComment;

public sealed record AddCommentCommand(Guid TaskRunId, string Body) : ICommand<AddCommentResult>;

public sealed class AddCommentValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentValidator()
    {
        RuleFor(c => c.TaskRunId).NotEmpty();
        RuleFor(c => c.Body).NotEmpty().MaximumLength(10000);
    }
}
