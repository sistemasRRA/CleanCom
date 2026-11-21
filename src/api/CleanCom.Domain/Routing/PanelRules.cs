namespace CleanCom.Domain.Routing;

/// <summary>Where a ruta is in its day (Panel del día).</summary>
public enum RutaProgress
{
    /// <summary>Nothing started yet.</summary>
    SinSalir,
    /// <summary>Some parada started or closed, and some still to visit.</summary>
    EnRuta,
    /// <summary>Every parada closed: going back to the base.</summary>
    DeRegreso,
}

/// <summary>
/// Panel del día (docs/03-reglas-de-negocio.md, Panel del día): progress of each ruta, arrivals on time, delay and
/// a chofer without GPS.
/// </summary>
public static class PanelRules
{
    public static bool IsClosed(ParadaState s) => s is ParadaState.Realizada or ParadaState.NoRealizada;

    public static RutaProgress Progress(IReadOnlyCollection<ParadaState> states) =>
        states.Count == 0 || states.All(s => s == ParadaState.Pendiente) ? RutaProgress.SinSalir
        : states.All(IsClosed) ? RutaProgress.DeRegreso
        : RutaProgress.EnRuta;

    /// <summary>An arrival is on time within ± Parada.ToleranciaLlegadaMinutos of its estimated time.</summary>
    public static bool OnTime(TimeOnly arrived, TimeOnly estimated, int toleranceMinutes) =>
        Math.Abs((arrived.ToTimeSpan() - estimated.ToTimeSpan()).TotalMinutes) <= toleranceMinutes;

    /// <summary>
    /// Minutes behind schedule: now is past the estimated arrival of the next parada still to visit by more than the
    /// tolerance. Null when on time or nothing is left.
    /// </summary>
    public static int? LateMinutes(TimeOnly now, TimeOnly? nextEstimated, int toleranceMinutes)
    {
        if (nextEstimated is not { } estimated || now <= estimated)
        {
            return null;
        }
        var late = (int)(now.ToTimeSpan() - estimated.ToTimeSpan()).TotalMinutes;
        return late > toleranceMinutes ? late : null;
    }

    /// <summary>A ruta on the road whose last GPS reading is older than Gps.SinSenalMinutos (or that never sent one).</summary>
    public static bool WithoutGps(RutaProgress progress, DateTimeOffset? lastReading, DateTimeOffset now, int minutes) =>
        progress == RutaProgress.EnRuta && (lastReading is null || (now - lastReading.Value).TotalMinutes > minutes);

    /// <summary>Whole percent, or null when there is nothing to measure.</summary>
    public static int? Percent(int part, int total) => total == 0 ? null : (int)Math.Round(100.0 * part / total);
}
