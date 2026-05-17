using FluentValidation.Results;

namespace Testers.Application.Exceptions;

/// <summary>
/// Thrown by <c>ValidationBehavior</c> when one or more FluentValidation rules fail.
/// The ExceptionHandler in the Api project maps this to <c>400 ProblemDetails</c> with
/// errors grouped by field name.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    /// <summary>Field name → list of error messages for that field.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
