namespace Testers.SharedKernel.Abstractions;

// Don't call DateTime.UtcNow in handlers/interceptors - inject this so tests can pin time.
public interface IClock
{
    DateTime UtcNow { get; }
}
