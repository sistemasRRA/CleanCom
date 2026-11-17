namespace CleanCom.Domain.Routing;

public readonly record struct GeoPoint(double Lat, double Lng);

/// <summary>A parada as the planner sees it. Times are minutes since midnight (Mexico City).</summary>
/// <param name="Location">Null when the parada has no coordinates: its travel time counts as zero and it is flagged.</param>
/// <param name="Position">Current order (1, 2, 3…); 0 when it has none yet.</param>
/// <param name="PositionFixed">Moved by hand: optimizing keeps it in <see cref="Position"/>.</param>
/// <param name="Started">The chofer already started it: it stays at the front.</param>
public record PlanStop(
    int Id,
    GeoPoint? Location,
    int ServiceMinutes,
    int WindowStart,
    int WindowEnd,
    ParadaPlacement Placement,
    int? ViajeId,
    int? ViajeStep,
    int Position,
    bool PositionFixed,
    bool Started,
    int Garments);

/// <summary>Values of the configuration that shape the day (docs/03-reglas-de-negocio.md, Configuración).</summary>
/// <param name="Departure">Ruta.SalidaBase, minutes since midnight.</param>
/// <param name="MaxReturn">Ruta.RegresoMaximo (or the Saturday one), minutes since midnight.</param>
/// <param name="SpeedKmh">Average speed to estimate travel until Google computes it.</param>
/// <param name="DistanceFactor">Street distance over straight-line distance.</param>
public record PlanSettings(
    GeoPoint Base,
    int Departure,
    int MaxReturn,
    int LunchFrom,
    int LunchMinutes,
    double SpeedKmh,
    double DistanceFactor,
    int Capacity);

/// <param name="Arrival">When the chofer starts the visit, after waiting for its window to open.</param>
/// <param name="LunchAfter">The lunch break goes right after this parada.</param>
public record PlannedStop(int Id, int Order, int Arrival, int Leave, int WaitMinutes, double KmFromPrevious, bool OutOfWindow, bool NoLocation, bool LunchAfter);

public record RoutePlan(IReadOnlyList<PlannedStop> Stops, int? LunchStart, double Km, int ReturnAt, bool Overtime, int Garments, bool OverCapacity);

/// <summary>
/// Estimated times of a ruta and an order of its own while Google Route Optimization is not connected
/// (docs/03-reglas-de-negocio.md, Rutas y optimización). Leaves the base at <see cref="PlanSettings.Departure"/>,
/// waits for each window to open, takes lunch once past <see cref="PlanSettings.LunchFrom"/> and returns to the base.
/// </summary>
public static class RoutePlanner
{
    private const double EarthRadiusKm = 6371;

    /// <summary>Straight-line distance in km.</summary>
    public static double Km(GeoPoint a, GeoPoint b)
    {
        static double Rad(double d) => d * Math.PI / 180;
        var dLat = Rad(b.Lat - a.Lat);
        var dLng = Rad(b.Lng - a.Lng);
        var h = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(Rad(a.Lat)) * Math.Cos(Rad(b.Lat)) * Math.Pow(Math.Sin(dLng / 2), 2);
        return 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(h));
    }

    /// <summary>Street km and minutes between two points; unknown points count as zero.</summary>
    public static (double Km, int Minutes) Travel(GeoPoint? from, GeoPoint? to, PlanSettings s)
    {
        if (from is not { } a || to is not { } b)
        {
            return (0, 0);
        }
        var km = Km(a, b) * s.DistanceFactor;
        return (km, (int)Math.Ceiling(km / s.SpeedKmh * 60));
    }

    /// <summary>Times for the paradas in the given order.</summary>
    public static RoutePlan Schedule(IReadOnlyList<PlanStop> ordered, PlanSettings s)
    {
        var stops = new List<PlannedStop>(ordered.Count);
        GeoPoint? here = s.Base;
        var now = s.Departure;
        double km = 0;
        int? lunch = null;

        for (var i = 0; i < ordered.Count; i++)
        {
            var stop = ordered[i];
            var (legKm, legMinutes) = Travel(here, stop.Location, s);
            km += legKm;
            var reach = now + legMinutes;
            var arrival = Math.Max(reach, stop.WindowStart);
            var leave = arrival + stop.ServiceMinutes;
            var lunchAfter = false;
            // Lunch goes after the parada that ends past LunchFrom, never between the pasos of a viaje.
            var nextIsSameViaje = i + 1 < ordered.Count && stop.ViajeId is not null && ordered[i + 1].ViajeId == stop.ViajeId;
            if (lunch is null && leave >= s.LunchFrom && !nextIsSameViaje && i + 1 < ordered.Count)
            {
                lunch = leave;
                lunchAfter = true;
                now = leave + s.LunchMinutes;
            }
            else
            {
                now = leave;
            }
            stops.Add(new PlannedStop(stop.Id, i + 1, arrival, leave, arrival - reach, Math.Round(legKm, 1),
                arrival > stop.WindowEnd, stop.Location is null, lunchAfter));
            if (stop.Location is not null)
            {
                here = stop.Location;
            }
        }

        var (backKm, backMinutes) = Travel(here, s.Base, s);
        km += backKm;
        var returnAt = now + backMinutes;
        var garments = ordered.Sum(x => x.Garments);
        return new RoutePlan(stops, lunch, Math.Round(km, 1), returnAt, returnAt > s.MaxReturn, garments, garments > s.Capacity);
    }

    /// <summary>
    /// An order of its own: started paradas first, then the ones pinned first, the rest by nearest feasible next
    /// stop (favoring windows that close sooner), and the ones pinned last. The pasos of a viaje keep their order:
    /// a paso can go only after the previous one. A parada moved by hand keeps its position.
    /// </summary>
    public static IReadOnlyList<PlanStop> Optimize(IReadOnlyList<PlanStop> stops, PlanSettings s)
    {
        var started = stops.Where(x => x.Started).OrderBy(x => x.Position).ToList();
        var pinned = stops.Where(x => !x.Started && x.PositionFixed && x.Position > 0).OrderBy(x => x.Position).ToList();
        var rest = stops.Except(started).Except(pinned).ToList();

        var order = new List<PlanStop>(started);
        GeoPoint? here = s.Base;
        var now = s.Departure;
        foreach (var placement in new[] { ParadaPlacement.Primera, ParadaPlacement.Automatica, ParadaPlacement.Ultima })
        {
            var pending = rest.Where(x => x.Placement == placement).ToList();
            while (pending.Count > 0)
            {
                // A paso is ready once the earlier pasos of its viaje are no longer pending in this group.
                var ready = pending.Where(x => x.ViajeId is not { } v
                    || !pending.Any(o => o.ViajeId == v && o.ViajeStep < x.ViajeStep)).ToList();
                if (ready.Count == 0)
                {
                    ready = pending;
                }
                var best = ready.MinBy(x => Score(x, here, now, s))!;
                pending.Remove(best);
                order.Add(best);
                (here, now) = After(best, here, now, s);
            }
        }

        // Paradas moved by hand go back to their position.
        foreach (var stop in pinned)
        {
            order.Insert(Math.Clamp(stop.Position - 1, 0, order.Count), stop);
        }
        return ProvisionalOrder.KeepViajeOrder(order, x => x.ViajeId, x => x.ViajeStep);
    }

    private static double Score(PlanStop stop, GeoPoint? here, int now, PlanSettings s)
    {
        var (_, minutes) = Travel(here, stop.Location, s);
        var reach = now + minutes;
        var arrival = Math.Max(reach, stop.WindowStart);
        var late = Math.Max(0, arrival - stop.WindowEnd);
        var slack = Math.Max(0, stop.WindowEnd - arrival);
        return (arrival - now) + late * 10 + slack * 0.2;
    }

    private static (GeoPoint? Here, int Now) After(PlanStop stop, GeoPoint? here, int now, PlanSettings s)
    {
        var (_, minutes) = Travel(here, stop.Location, s);
        now = Math.Max(now + minutes, stop.WindowStart) + stop.ServiceMinutes;
        return (stop.Location ?? here, now);
    }
}
