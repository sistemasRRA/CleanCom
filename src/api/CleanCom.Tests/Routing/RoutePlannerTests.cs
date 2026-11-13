using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Routing;

/// <summary>docs/03-reglas-de-negocio.md, Rutas y optimización, and the acceptance criteria of docs/pantallas/oficina/rutas.md.</summary>
public class RoutePlannerTests
{
    private static readonly GeoPoint Leones = new(19.3608092, -99.1968494);

    // 09:00 departure, 18:00 return, lunch 60 min from 13:00, 18 km/h, factor 1.4, 140 prendas.
    private static readonly PlanSettings Settings = new(Leones, 9 * 60, 18 * 60, 13 * 60, 60, 18, 1.4, 140);

    private static int H(int hour, int minute = 0) => hour * 60 + minute;

    /// <summary>A point about <paramref name="km"/> km north of Leones in a straight line.</summary>
    private static GeoPoint North(double km) => new(Leones.Lat + km / 111.2, Leones.Lng);

    private static PlanStop Stop(int id, double km, int windowStart = 9 * 60, int windowEnd = 18 * 60,
        ParadaPlacement placement = ParadaPlacement.Automatica, int? viaje = null, int? step = null,
        int position = 0, bool fixedPosition = false, bool started = false, int garments = 0, int service = 15) =>
        new(id, North(km), service, windowStart, windowEnd, placement, viaje, step, position, fixedPosition, started, garments);

    [Fact]
    public void Times_include_travel_service_and_the_return_to_the_base()
    {
        // 5 km straight = 7 km in the street = 24 min at 18 km/h.
        var plan = RoutePlanner.Schedule([Stop(1, 5)], Settings);
        var stop = Assert.Single(plan.Stops);
        Assert.Equal(H(9, 24), stop.Arrival);
        Assert.Equal(H(9, 39), stop.Leave);
        Assert.Equal(H(10, 3), plan.ReturnAt);
        Assert.Equal(14, plan.Km, 0);
        Assert.False(plan.Overtime);
    }

    [Fact]
    public void An_afternoon_parada_waits_for_its_window()
    {
        var plan = RoutePlanner.Schedule([Stop(1, 1, H(13), H(18))], Settings);
        Assert.Equal(H(13), plan.Stops[0].Arrival);
        Assert.True(plan.Stops[0].WaitMinutes > 200);
    }

    [Fact]
    public void A_return_after_18_00_is_overtime()
    {
        // Ruta F today returns at 19:36: a parada that starts at 17:50 and takes an hour.
        var plan = RoutePlanner.Schedule([Stop(1, 2, H(17, 50), H(18), service: 60)], Settings);
        Assert.True(plan.ReturnAt > H(18));
        Assert.True(plan.Overtime);
    }

    [Fact]
    public void A_parada_after_its_window_is_flagged()
    {
        // Wildress Roma at 18:24 with a 4:00 p.m. limit.
        var plan = RoutePlanner.Schedule([Stop(1, 1, H(16, 30), H(18)), Stop(2, 1, H(9), H(16))], Settings);
        Assert.True(plan.Stops[1].OutOfWindow);
        Assert.False(plan.Stops[0].OutOfWindow);
    }

    [Fact]
    public void Lunch_goes_once_after_the_parada_that_ends_past_13_00_but_not_inside_a_viaje()
    {
        var plan = RoutePlanner.Schedule(
        [
            Stop(1, 1, H(12, 30), H(18)),
            Stop(2, 1, viaje: 9, step: 1),
            Stop(3, 1, viaje: 9, step: 2),
            Stop(4, 1),
        ], Settings);
        Assert.Null(plan.Stops.FirstOrDefault(s => s.LunchAfter && s.Id is 1 or 2));
        Assert.True(plan.Stops.Single(s => s.Id == 3).LunchAfter);
        Assert.Equal(1, plan.Stops.Count(s => s.LunchAfter));
        var after = plan.Stops.Single(s => s.Id == 4);
        Assert.True(after.Arrival >= plan.LunchStart + 60);
    }

    [Fact]
    public void Prendas_over_the_unit_capacity_are_flagged()
    {
        var plan = RoutePlanner.Schedule([Stop(1, 1, garments: 90), Stop(2, 2, garments: 60)], Settings);
        Assert.Equal(150, plan.Garments);
        Assert.True(plan.OverCapacity);
    }

    [Fact]
    public void A_viaje_of_3_pasos_stays_in_order_after_optimizing()
    {
        var stops = new[]
        {
            Stop(1, 8),
            Stop(10, 1, viaje: 5, step: 3),
            Stop(2, 2),
            Stop(11, 6, viaje: 5, step: 1),
            Stop(3, 4),
            Stop(12, 3, viaje: 5, step: 2),
        };
        var order = RoutePlanner.Optimize(stops, Settings).Select(s => s.Id).ToList();
        Assert.True(order.IndexOf(11) < order.IndexOf(12) && order.IndexOf(12) < order.IndexOf(10), string.Join(",", order));
        Assert.Equal(6, order.Count);
    }

    [Fact]
    public void A_vuelta_loaded_in_the_morning_is_delivered_in_the_afternoon_with_other_paradas_between()
    {
        // Operadora: paso 1 loads in Leones before 12:00, paso 2 delivers in the afternoon before 14:00.
        var stops = new[]
        {
            Stop(20, 0, H(9), H(12), viaje: 7, step: 1),
            Stop(21, 15, H(13), H(14), viaje: 7, step: 2),
            Stop(1, 2, H(9), H(13)),
            Stop(2, 3, H(9), H(13)),
        };
        var order = RoutePlanner.Optimize(stops, Settings).Select(s => s.Id).ToList();
        Assert.Equal(20, order[0]);
        Assert.Equal(21, order[^1]);
        var plan = RoutePlanner.Schedule(order.Select(id => stops.Single(s => s.Id == id)).ToList(), Settings);
        Assert.All(plan.Stops, s => Assert.False(s.OutOfWindow));
    }

    [Fact]
    public void A_pinned_parada_keeps_its_position_when_optimizing()
    {
        var stops = new[]
        {
            Stop(1, 1, position: 4),
            Stop(2, 9, position: 1, fixedPosition: true),
            Stop(3, 2, position: 2),
            Stop(4, 3, position: 3),
        };
        var order = RoutePlanner.Optimize(stops, Settings).Select(s => s.Id).ToList();
        Assert.Equal(2, order[0]);
        // The rest by distance: nearest first.
        Assert.Equal([1, 3, 4], order.Skip(1));
    }

    [Fact]
    public void Optimizing_visits_the_nearest_first_and_respects_first_last_and_started()
    {
        var stops = new[]
        {
            Stop(1, 6),
            Stop(2, 1),
            Stop(3, 3),
            Stop(4, 10, placement: ParadaPlacement.Primera),
            Stop(5, 0.5, placement: ParadaPlacement.Ultima),
            Stop(6, 12, position: 1, started: true),
        };
        var order = RoutePlanner.Optimize(stops, Settings).Select(s => s.Id).ToList();
        Assert.Equal([6, 4, 1, 3, 2, 5], order);
    }

    [Fact]
    public void Morning_windows_go_before_any_time_paradas_that_are_not_much_closer()
    {
        var stops = new[] { Stop(1, 2), Stop(2, 3, H(9), H(13)) };
        var order = RoutePlanner.Optimize(stops, Settings).Select(s => s.Id).ToList();
        Assert.Equal([2, 1], order);
    }
}
