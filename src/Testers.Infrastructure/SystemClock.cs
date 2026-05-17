using Testers.Application.Abstractions;

namespace Testers.Infrastructure;

/// <summary>
/// Real-clock implementation of <see cref="IClock"/>. Registered singleton in DI.
/// Tests inject a fake clock (e.g. fixed timestamp) directly instead of substituting this class.
/// </summary>
internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
