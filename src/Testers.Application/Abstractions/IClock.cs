namespace Testers.Application.Abstractions;

/// <summary>
/// Abstraction over wall-clock time so handlers and interceptors don't call
/// <c>DateTime.UtcNow</c> directly. Lets tests inject a deterministic clock
/// (e.g. always returns 2026-01-01T00:00:00Z). Infrastructure provides a SystemClock.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
