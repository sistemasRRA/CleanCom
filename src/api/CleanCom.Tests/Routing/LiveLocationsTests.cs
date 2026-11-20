using CleanCom.Application.Common;
using CleanCom.Application.Tracking;

namespace CleanCom.Tests.Routing;

/// <summary>The last position of each chofer, kept in memory for the live map.</summary>
public class LiveLocationsTests
{
    private static readonly DateTimeOffset Ten = new(2026, 11, 10, 16, 0, 0, TimeSpan.Zero);

    private static LiveLocationDto Location(int rutaDiaId, DateOnly date, DateTimeOffset at) =>
        new(rutaDiaId, date, 1, "F", "#1565c0", "Juan", 19.36, -99.19, at, 10);

    [Fact]
    public void An_older_position_does_not_replace_a_newer_one()
    {
        var live = new LiveLocations();
        var day = new DateOnly(2026, 11, 10);
        live.Set(Location(1, day, Ten.AddMinutes(5)));
        live.Set(Location(1, day, Ten));

        Assert.Equal(Ten.AddMinutes(5), live.Get(1)!.At);
    }

    [Fact]
    public void A_new_day_drops_the_positions_of_the_days_before()
    {
        var live = new LiveLocations();
        live.Set(Location(1, new DateOnly(2026, 11, 10), Ten));
        live.Set(Location(2, new DateOnly(2026, 11, 11), Ten.AddDays(1)));

        Assert.Null(live.Get(1));
        Assert.NotNull(live.Get(2));
    }
}
