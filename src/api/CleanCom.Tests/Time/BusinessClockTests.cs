using CleanCom.Domain.Time;
using CleanCom.Infrastructure.Time;

namespace CleanCom.Tests.Time;

/// <summary>TimeProvider with whatever time the test needs.</summary>
public sealed class FixedTime(DateTimeOffset utc) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utc;

    /// <summary>Fixed time from a Mexico City date and time.</summary>
    public static FixedTime InMexicoCity(int year, int month, int day, int hour, int minute) =>
        new(BusinessTime.ToUtc(new DateOnly(year, month, day), new TimeOnly(hour, minute)));
}

public class BusinessClockTests
{
    [Fact]
    public void Mexico_City_is_UTC_minus_6_all_year()
    {
        // Mexico City has had no daylight saving time since 2022.
        var winter = BusinessTime.ToUtc(new DateOnly(2026, 1, 15), new TimeOnly(13, 0));
        var summer = BusinessTime.ToUtc(new DateOnly(2026, 7, 15), new TimeOnly(13, 0));
        Assert.Equal(new DateTimeOffset(2026, 1, 15, 19, 0, 0, TimeSpan.Zero), winter);
        Assert.Equal(new DateTimeOffset(2026, 7, 15, 19, 0, 0, TimeSpan.Zero), summer);
    }

    [Fact]
    public void The_business_date_is_Mexico_Citys_not_UTCs()
    {
        // November 2 at 23:30 in Mexico City is already November 3 in UTC.
        var clock = new BusinessClock(FixedTime.InMexicoCity(2026, 11, 2, 23, 30));
        Assert.Equal(new DateOnly(2026, 11, 3), DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
        Assert.Equal(new DateOnly(2026, 11, 2), clock.Today);
        Assert.Equal(new TimeOnly(23, 30), TimeOnly.FromDateTime(clock.LocalNow.DateTime));
        Assert.Equal(TimeSpan.FromHours(-6), clock.LocalNow.Offset);
    }

    [Fact]
    public void UtcNow_is_stored_with_zero_offset()
    {
        var clock = new BusinessClock(FixedTime.InMexicoCity(2026, 11, 2, 12, 1));
        Assert.Equal(TimeSpan.Zero, clock.UtcNow.Offset);
        Assert.Equal(new TimeOnly(12, 1), BusinessTime.TimeOfDay(clock.UtcNow));
    }
}
