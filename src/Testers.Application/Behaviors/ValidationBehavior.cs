using FluentValidation;
using Testers.Application.Abstractions;
using ValidationException = Testers.Application.Exceptions.ValidationException;

namespace Testers.Application.Behaviors;

/// <summary>
/// Runs all FluentValidation <c>IValidator&lt;TRequest&gt;</c>s registered for the request type
/// in parallel. Fails fast with <see cref="ValidationException"/> (mapped to 400 + ProblemDetails
/// by the Api project's ExceptionHandler) so handlers never see invalid input.
///
/// Validators are auto-registered by <c>AddApplication()</c> via
/// <c>AddValidatorsFromAssemblyContaining</c> — feature slices just define a class implementing
/// <c>AbstractValidator&lt;TheirCommand&gt;</c> next to their handler.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToArray();

        if (failures.Length > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
