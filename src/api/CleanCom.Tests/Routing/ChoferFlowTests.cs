using CleanCom.Domain.Common;
using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Routing;

/// <summary>"En camino" and "Llegué" (docs/pantallas/apps/chofer.md, Flujo del turno; Visita.OrdenObligatorio, Gps.ToleranciaMetros).</summary>
public class ChoferFlowTests
{
    private static List<FlowParada> Ruta(params ParadaState[] states) =>
        states.Select((s, i) => new FlowParada(i + 1, i + 1, $"Parada {i + 1}", s)).ToList();

    [Fact]
    public void The_next_parada_is_the_first_pending_in_order()
    {
        var ruta = Ruta(ParadaState.Realizada, ParadaState.EnSitio, ParadaState.Pendiente, ParadaState.Pendiente);
        Assert.Equal(3, ChoferFlow.Next(ruta)!.Value.Id);
        Assert.Null(ChoferFlow.Next(Ruta(ParadaState.Realizada)));
    }

    [Fact]
    public void With_the_order_required_only_the_next_parada_can_start()
    {
        var ruta = Ruta(ParadaState.Pendiente, ParadaState.Pendiente);
        var error = Assert.Throws<DomainException>(() => ChoferFlow.EnsureCanStart(ruta[1], ruta, orderRequired: true));
        Assert.Contains("sigue la 1 (Parada 1)", error.Message);
        ChoferFlow.EnsureCanStart(ruta[0], ruta, orderRequired: true);
        // Visita.OrdenObligatorio off: any pending parada.
        ChoferFlow.EnsureCanStart(ruta[1], ruta, orderRequired: false);
    }

    [Fact]
    public void Only_one_parada_en_camino_at_a_time()
    {
        var ruta = Ruta(ParadaState.EnCamino, ParadaState.Pendiente);
        var error = Assert.Throws<DomainException>(() => ChoferFlow.EnsureCanStart(ruta[1], ruta, orderRequired: false));
        Assert.Contains("Ya vas en camino a la parada 1", error.Message);
    }

    [Fact]
    public void A_visit_waiting_to_be_closed_does_not_block_the_next_one()
    {
        // Closing the visit arrives in week 4; meanwhile EnSitio does not stop the chofer.
        var ruta = Ruta(ParadaState.EnSitio, ParadaState.Pendiente);
        ChoferFlow.EnsureCanStart(ruta[1], ruta, orderRequired: true);
    }

    [Fact]
    public void Arriving_is_allowed_en_camino_or_straight_from_pending_but_not_twice()
    {
        var ruta = Ruta(ParadaState.EnCamino, ParadaState.Pendiente);
        ChoferFlow.EnsureCanArrive(ruta[0], ruta, orderRequired: true);
        Assert.Throws<DomainException>(() => ChoferFlow.EnsureCanArrive(ruta[1], ruta, orderRequired: true));

        var straight = Ruta(ParadaState.Pendiente);
        ChoferFlow.EnsureCanArrive(straight[0], straight, orderRequired: true);

        var twice = Ruta(ParadaState.EnSitio);
        Assert.Throws<DomainException>(() => ChoferFlow.EnsureCanArrive(twice[0], twice, orderRequired: true));
    }

    [Fact]
    public void Arrival_is_checked_against_the_gps_tolerance()
    {
        var parada = new GeoPoint(19.3608092, -99.1968494);
        var near = new GeoPoint(19.3608092 + 0.002, -99.1968494); // about 220 m north
        var far = new GeoPoint(19.3608092 + 0.01, -99.1968494);   // about 1.1 km

        var nearMeters = ChoferFlow.DistanceMeters(parada, near);
        Assert.InRange(nearMeters, 200, 240);
        Assert.True(ChoferFlow.WithinTolerance(nearMeters, 300));
        Assert.False(ChoferFlow.WithinTolerance(ChoferFlow.DistanceMeters(parada, far), 300));
        // Without GPS or coordinates the distance is unknown and is accepted.
        Assert.True(ChoferFlow.WithinTolerance(null, 300));
    }
}
