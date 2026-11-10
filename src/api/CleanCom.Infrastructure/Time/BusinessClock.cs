using CleanCom.Domain.Time;

namespace CleanCom.Infrastructure.Time;

/// <summary>
/// Real clock. Tests pass a <see cref="TimeProvider"/> with whatever time they need.
/// </summary>
public class BusinessClock(TimeProvider provider) : IBusinessClock
{
    public DateTimeOffset UtcNow => provider.GetUtcNow();
    public DateTimeOffset LocalNow => BusinessTime.ToLocal(UtcNow);
    public DateOnly Today => BusinessTime.Date(UtcNow);
}
