using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Routing;

/// <summary>Panel del día (docs/03-reglas-de-negocio.md): progress, arrivals on time, delay and GPS.</summary>
public class PanelRulesTests
{
    private static TimeOnly T(int h, int m) => new(h, m);

    [Fact]
    public void A_ruta_is_sin_salir_until_something_starts_and_de_regreso_when_everything_closed()
    {
        Assert.Equal(RutaProgress.SinSalir, PanelRules.Progress([ParadaState.Pendiente, ParadaState.Pendiente]));
        Assert.Equal(RutaProgress.SinSalir, PanelRules.Progress([]));
        Assert.Equal(RutaProgress.EnRuta, PanelRules.Progress([ParadaState.EnCamino, ParadaState.Pendiente]));
        Assert.Equal(RutaProgress.EnRuta, PanelRules.Progress([ParadaState.Realizada, ParadaState.Pendiente]));
        Assert.Equal(RutaProgress.DeRegreso, PanelRules.Progress([ParadaState.Realizada, ParadaState.NoRealizada]));
    }

    [Fact]
    public void An_arrival_is_on_time_within_15_minutes_either_way()
    {
        // Estimated 10:30: 10:15 and 10:45 count, 10:46 and 10:14 do not.
        Assert.True(PanelRules.OnTime(T(10, 15), T(10, 30), 15));
        Assert.True(PanelRules.OnTime(T(10, 45), T(10, 30), 15));
        Assert.False(PanelRules.OnTime(T(10, 46), T(10, 30), 15));
        Assert.False(PanelRules.OnTime(T(10, 14), T(10, 30), 15));
    }

    [Fact]
    public void The_ruta_is_late_when_the_next_parada_is_more_than_15_minutes_past_its_estimate()
    {
        Assert.Null(PanelRules.LateMinutes(T(10, 40), T(10, 30), 15));
        Assert.Equal(16, PanelRules.LateMinutes(T(10, 46), T(10, 30), 15));
        Assert.Null(PanelRules.LateMinutes(T(10, 0), T(10, 30), 15));
        Assert.Null(PanelRules.LateMinutes(T(12, 0), null, 15));
    }

    [Fact]
    public void A_ruta_on_the_road_without_a_reading_for_10_minutes_has_no_gps()
    {
        var now = new DateTimeOffset(2026, 11, 20, 17, 0, 0, TimeSpan.Zero);
        Assert.True(PanelRules.WithoutGps(RutaProgress.EnRuta, now.AddMinutes(-11), now, 10));
        Assert.False(PanelRules.WithoutGps(RutaProgress.EnRuta, now.AddMinutes(-9), now, 10));
        Assert.True(PanelRules.WithoutGps(RutaProgress.EnRuta, null, now, 10));
        // Before leaving or on the way back nobody is followed.
        Assert.False(PanelRules.WithoutGps(RutaProgress.SinSalir, null, now, 10));
    }

    [Fact]
    public void The_last_position_counts_as_old_after_the_no_signal_minutes()
    {
        // Last reading at 17:00 (11:00 in Mexico City) with Gps.SinSenalMinutos 10 → old from 17:10.
        var at = new DateTimeOffset(2026, 11, 20, 17, 0, 0, TimeSpan.Zero);
        Assert.Equal(at.AddMinutes(10), PanelRules.SignalLostAt(at, 10));
    }

    [Fact]
    public void Percent_is_rounded_and_empty_without_data()
    {
        Assert.Equal(67, PanelRules.Percent(2, 3));
        Assert.Null(PanelRules.Percent(0, 0));
    }
}
