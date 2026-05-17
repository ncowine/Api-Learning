using FluentValidation;
using Testers.SharedKernel.Abstractions;
using ValidationException = Testers.SharedKernel.Exceptions.ValidationException;

namespace Testers.SharedKernel.Behaviors;

// Runs validators in parallel. Throws our ValidationException (mapped to 400 by Api).
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToArray();

        if (failures.Length > 0) throw new ValidationException(failures);
        return await next();
    }
}
