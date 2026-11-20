using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Routing;

/// <summary>
/// GPS del chofer (docs/03-reglas-de-negocio.md): readings outside the day or less accurate than Gps.ToleranciaMetros
/// are not used; the history keeps one every Gps.HistorialSegundos (120) or when the chofer moved Gps.HistorialMetros (100).
/// </summary>
public class LocationTrailTests
{
    private const int Tolerance = 300;
    private const int Seconds = 120;
    private const int Meters = 100;

    // 10 Nov 2026 in Mexico City (UTC-6).
    private static readonly DateTimeOffset DayStart = new(2026, 11, 10, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Ten = DayStart.AddHours(10);

    // Leones; 0.001° of latitude is about 111 m.
    private static LocationReading At(DateTimeOffset at, double northMeters = 0, int? accuracy = 10) =>
        new(19.3608 + northMeters / 111_000.0, -99.1968, at, accuracy);

    [Fact]
    public void Readings_from_another_day_in_the_future_or_too_inaccurate_are_not_used()
    {
        var now = Ten;
        var readings = new[]
        {
            At(DayStart.AddMinutes(-1)), // yesterday, kept without signal
            At(now.AddMinutes(1)), // the phone's clock is ahead
            At(now.AddMinutes(-5), accuracy: 301), // worse than the tolerance
            At(now.AddMinutes(-2), accuracy: 300),
            At(now.AddMinutes(-4), accuracy: null), // unknown accuracy is used
        };

        var usable = LocationTrail.Usable(readings, DayStart, now, Tolerance);

        Assert.Equal([now.AddMinutes(-4), now.AddMinutes(-2)], usable.Select(r => r.At));
    }

    [Fact]
    public void The_first_reading_of_the_day_is_kept()
    {
        var kept = LocationTrail.ToKeep(null, [At(Ten)], Seconds, Meters);
        Assert.Single(kept);
    }

    [Fact]
    public void A_reading_is_kept_every_120_seconds_while_the_chofer_stands_still()
    {
        // Readings every 30 s at the same spot: only the ones 2 minutes apart.
        var readings = Enumerable.Range(1, 8).Select(i => At(Ten.AddSeconds(30 * i))).ToList();

        var kept = LocationTrail.ToKeep(At(Ten), readings, Seconds, Meters);

        Assert.Equal([Ten.AddSeconds(120), Ten.AddSeconds(240)], kept.Select(r => r.At));
    }

    [Fact]
    public void Moving_100_meters_keeps_the_reading_before_the_120_seconds()
    {
        var kept = LocationTrail.ToKeep(At(Ten), [At(Ten.AddSeconds(30), northMeters: 90), At(Ten.AddSeconds(60), northMeters: 150)], Seconds, Meters);

        Assert.Equal([Ten.AddSeconds(60)], kept.Select(r => r.At));
    }

    [Fact]
    public void A_resend_after_losing_signal_is_not_kept_twice()
    {
        var kept = LocationTrail.ToKeep(At(Ten), [At(Ten), At(Ten.AddSeconds(-300), northMeters: 500)], Seconds, Meters);
        Assert.Empty(kept);
    }

    [Fact]
    public void The_live_position_is_the_newest_reading_unless_a_newer_one_is_shown()
    {
        var shown = At(Ten.AddMinutes(5));
        // Readings saved without signal arrive late: they go to the history but do not move the pin back.
        Assert.Equal(shown, LocationTrail.Latest(shown, [At(Ten), At(Ten.AddMinutes(1))]));
        Assert.Equal(Ten.AddMinutes(6), LocationTrail.Latest(shown, [At(Ten.AddMinutes(6))])!.Value.At);
        Assert.Null(LocationTrail.Latest(null, []));
    }
}
