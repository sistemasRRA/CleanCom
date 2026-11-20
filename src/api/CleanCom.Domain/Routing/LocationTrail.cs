namespace CleanCom.Domain.Routing;

/// <summary>A GPS reading of the chofer's phone.</summary>
public readonly record struct LocationReading(double Lat, double Lng, DateTimeOffset At, int? AccuracyMeters);

/// <summary>
/// Which GPS readings of the chofer are used (docs/03-reglas-de-negocio.md, GPS del chofer). The phone sends one every
/// 30 to 60 seconds during the shift; the last one feeds the live map and only some go to the history, so the table
/// stays small: one every Gps.HistorialSegundos, or sooner when the chofer moved Gps.HistorialMetros.
/// </summary>
public static class LocationTrail
{
    /// <summary>
    /// Readings worth using: within the day of the ruta and not in the future, and no less accurate than
    /// Gps.ToleranciaMetros (a reading that bad could not even validate an arrival). Sorted by time.
    /// </summary>
    public static IReadOnlyList<LocationReading> Usable(
        IEnumerable<LocationReading> readings, DateTimeOffset dayStart, DateTimeOffset now, int toleranceMeters) =>
        readings
            .Where(r => r.At >= dayStart && r.At <= now)
            .Where(r => r.AccuracyMeters is null || r.AccuracyMeters <= toleranceMeters)
            .OrderBy(r => r.At)
            .ToList();

    /// <summary>
    /// The readings that go to the history, given the last one already kept: the first of the day, then one when
    /// <paramref name="minSeconds"/> have passed or the chofer moved <paramref name="minMeters"/> or more.
    /// <paramref name="readings"/> must be sorted by time (see <see cref="Usable"/>).
    /// </summary>
    public static IReadOnlyList<LocationReading> ToKeep(
        LocationReading? lastKept, IReadOnlyList<LocationReading> readings, int minSeconds, int minMeters)
    {
        var kept = new List<LocationReading>();
        var last = lastKept;
        foreach (var reading in readings)
        {
            if (last is { } previous && reading.At <= previous.At)
            {
                // Already covered (a resend after losing signal) or older than what is kept.
                continue;
            }
            if (last is null
                || (reading.At - last.Value.At).TotalSeconds >= minSeconds
                || DistanceMeters(last.Value, reading) >= minMeters)
            {
                kept.Add(reading);
                last = reading;
            }
        }
        return kept;
    }

    /// <summary>The live position: the newest reading, unless the one already shown is newer.</summary>
    public static LocationReading? Latest(LocationReading? current, IReadOnlyList<LocationReading> readings) =>
        readings.Count == 0 ? current
        : current is { } c && c.At >= readings[^1].At ? current
        : readings[^1];

    private static int DistanceMeters(LocationReading a, LocationReading b) =>
        ChoferFlow.DistanceMeters(new GeoPoint(a.Lat, a.Lng), new GeoPoint(b.Lat, b.Lng));
}
