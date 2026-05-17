using Testers.SharedKernel.Abstractions;

namespace Testers.Infrastructure;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
