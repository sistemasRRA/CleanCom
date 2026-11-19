using CleanCom.Application.Chofer;
using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Routing;

/// <summary>docs/pantallas/apps/chofer.md, Flujo del turno, step 5 (cerrar la visita).</summary>
public class VisitClosingTests
{
    private static readonly FlowParada[] Ruta =
    [
        new(1, 1, "Ana", ParadaState.Realizada),
        new(2, 2, "Beto", ParadaState.EnSitio),
        new(3, 3, "Carla", ParadaState.Pendiente),
    ];

    [Fact]
    public void A_closed_visit_cannot_be_closed_again()
    {
        var error = Assert.Throws<DomainException>(() => VisitClosing.EnsureCanClose(Ruta[0], Ruta, orderRequired: true));
        Assert.Equal("Esta visita ya se cerró.", error.Message);
    }

    [Fact]
    public void A_visit_in_sitio_closes_and_a_later_one_must_wait_its_turn_when_order_is_required()
    {
        VisitClosing.EnsureCanClose(Ruta[1], Ruta, orderRequired: true);
        // Paradas 2 is en sitio, not pending: the next pending one is 3, so 3 can be closed directly.
        VisitClosing.EnsureCanClose(Ruta[2], Ruta, orderRequired: true);
        FlowParada[] other = [new(1, 1, "Ana", ParadaState.Pendiente), new(2, 2, "Beto", ParadaState.Pendiente)];
        Assert.Throws<DomainException>(() => VisitClosing.EnsureCanClose(other[1], other, orderRequired: true));
        VisitClosing.EnsureCanClose(other[1], other, orderRequired: false);
    }

    [Fact]
    public void The_photo_is_required_when_the_setting_says_so()
    {
        var data = new VisitCloseData(5, null, null, Photos: 0);
        Assert.Throws<DomainException>(() => VisitClosing.EnsureComplete(ParadaKind.Recoleccion, true, data, photoRequired: true));
        VisitClosing.EnsureComplete(ParadaKind.Recoleccion, true, data, photoRequired: false);
    }

    [Fact]
    public void A_recoleccion_of_a_pedido_needs_the_prendas()
    {
        var error = Assert.Throws<DomainException>(() =>
            VisitClosing.EnsureComplete(ParadaKind.Recoleccion, true, new VisitCloseData(null, null, null, 1), true));
        Assert.Equal("Escribe cuántas prendas recogiste.", error.Message);
        // A traslado only confirms.
        VisitClosing.EnsureComplete(ParadaKind.Traslado, false, new VisitCloseData(null, null, null, 1), true);
    }

    [Fact]
    public void The_reference_payment_needs_forma_and_monto()
    {
        Assert.Throws<DomainException>(() =>
            VisitClosing.EnsureComplete(ParadaKind.Entrega, true, new VisitCloseData(null, PaymentMethod.Efectivo, null, 1), true));
        Assert.Throws<DomainException>(() =>
            VisitClosing.EnsureComplete(ParadaKind.Entrega, true, new VisitCloseData(null, PaymentMethod.Efectivo, 0, 1), true));
        VisitClosing.EnsureComplete(ParadaKind.Entrega, true, new VisitCloseData(null, PaymentMethod.Efectivo, 350, 1), true);
        VisitClosing.EnsureComplete(ParadaKind.Entrega, true, new VisitCloseData(null, null, null, 1), true);
    }

    [Fact]
    public void Not_done_needs_a_motivo_and_the_photo()
    {
        Assert.Throws<DomainException>(() => VisitClosing.EnsureNotDone(null, " ", 1, true));
        Assert.Throws<DomainException>(() => VisitClosing.EnsureNotDone(null, "No había nadie", 0, true));
        VisitClosing.EnsureNotDone(null, "No había nadie", 1, true);
        VisitClosing.EnsureNotDone(4, null, 0, false);
    }

    [Fact]
    public void Done_moves_recoleccion_to_recolectado_and_entrega_to_entregado()
    {
        Assert.Equal(PedidoEtapa.Recolectado, VisitClosing.EtapaAfter(ParadaKind.Recoleccion));
        Assert.Equal(PedidoEtapa.Entregado, VisitClosing.EtapaAfter(ParadaKind.Entrega));
        Assert.Null(VisitClosing.EtapaAfter(ParadaKind.Traslado));
    }

    [Fact]
    public void The_signature_is_drawn_from_numbers_only()
    {
        var svg = ChoferVisitService.SignatureSvg(new SignatureRequest(1, null, 300, 150, [[10, 10, 50.25, 60, 400, -5]], null));
        Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 300 150\"", svg);
        // Points outside the canvas are clamped.
        Assert.Contains("M10 10 L50.3 60 L300 0", svg);
        Assert.Throws<DomainException>(() => ChoferVisitService.SignatureSvg(new SignatureRequest(1, null, 300, 150, [[1, 1]], null)));
    }
}

/// <summary>docs/pantallas/apps/chofer.md, Requisitos técnicos (sin señal).</summary>
public class ActionTimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 20, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DayStart = new(2026, 11, 17, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_action_saved_without_signal_keeps_the_time_it_happened()
    {
        var earlier = Now.AddHours(-2);
        Assert.Equal(earlier, ChoferFlow.ActionTime(earlier, Now, DayStart));
    }

    [Fact]
    public void A_time_in_the_future_or_before_the_day_is_taken_as_now()
    {
        Assert.Equal(Now, ChoferFlow.ActionTime(Now.AddMinutes(5), Now, DayStart));
        Assert.Equal(Now, ChoferFlow.ActionTime(DayStart.AddMinutes(-1), Now, DayStart));
        Assert.Equal(Now, ChoferFlow.ActionTime(null, Now, DayStart));
    }
}
