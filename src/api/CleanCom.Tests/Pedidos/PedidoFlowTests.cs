using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;

namespace CleanCom.Tests.Pedidos;

/// <summary>docs/03-reglas-de-negocio.md, Pedidos › Etapas and Atraso.</summary>
public class PedidoFlowTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 17);
    private static string Format(DateOnly d) => d.ToString("dd/MM");

    [Fact]
    public void Moving_forward_by_hand_needs_no_reason_and_goes_to_the_history()
    {
        var pedido = new Pedido { Etapa = PedidoEtapa.Recolectado };
        var evento = PedidoFlow.Move(pedido, PedidoEtapa.EnPlanta, null, Now);
        Assert.Equal(PedidoEtapa.EnPlanta, pedido.Etapa);
        Assert.Equal(Now, pedido.EtapaSince);
        Assert.Equal((PedidoEtapa.Recolectado, PedidoEtapa.EnPlanta, false), (evento.FromEtapa!.Value, evento.ToEtapa!.Value, evento.Automatic));
        Assert.Same(evento, Assert.Single(pedido.Eventos));
    }

    [Theory]
    [InlineData(PedidoEtapa.Listo, PedidoEtapa.EnPlanta)]
    [InlineData(PedidoEtapa.EntregaConfirmada, PedidoEtapa.Entregado)]
    [InlineData(PedidoEtapa.Agendado, PedidoEtapa.Cancelado)]
    [InlineData(PedidoEtapa.Cancelado, PedidoEtapa.Agendado)]
    public void Going_back_delivering_or_cancelling_by_hand_needs_a_reason(PedidoEtapa from, PedidoEtapa to)
    {
        var pedido = new Pedido { Etapa = from };
        Assert.Throws<DomainException>(() => PedidoFlow.Move(pedido, to, "  ", Now));
        Assert.Equal(from, pedido.Etapa);
        var evento = PedidoFlow.Move(pedido, to, "El invitado pidió revisar una prenda", Now);
        Assert.Equal("El invitado pidió revisar una prenda", evento.Note);
    }

    [Fact]
    public void Events_only_move_forward_and_never_reopen_a_cancelled_pedido()
    {
        var pedido = new Pedido { Etapa = PedidoEtapa.Listo };
        Assert.Null(PedidoFlow.Advance(pedido, PedidoEtapa.EnPlanta, Now));
        Assert.Equal(PedidoEtapa.Listo, pedido.Etapa);
        Assert.NotNull(PedidoFlow.Advance(pedido, PedidoEtapa.EnCamino, Now));
        Assert.True(pedido.Eventos.Single().Automatic);

        var cancelled = new Pedido { Etapa = PedidoEtapa.Cancelado };
        Assert.Null(PedidoFlow.Advance(cancelled, PedidoEtapa.Recolectado, Now));
    }

    [Fact]
    public void A_recoleccion_date_that_passed_is_late_and_today_is_yellow()
    {
        var late = new Pedido { Etapa = PedidoEtapa.Agendado, RecoleccionDate = Today.AddDays(-6) };
        var delay = PedidoFlow.Delay(late, Today, null, 3, Format);
        Assert.Equal(DelayLevel.Late, delay.Level);
        Assert.Equal("Atrasado 6 días · tocaba la recolección el 11/11", delay.Text);

        var due = new Pedido { Etapa = PedidoEtapa.Agendado, RecoleccionDate = Today };
        Assert.Equal(DelayLevel.Warning, PedidoFlow.Delay(due, Today, null, 3, Format).Level);
    }

    [Fact]
    public void An_entrega_date_that_passed_is_late()
    {
        var pedido = new Pedido { Etapa = PedidoEtapa.EntregaConfirmada, EntregaDate = Today.AddDays(-1) };
        Assert.Equal("Atrasado 1 día · tocaba la entrega el 16/11", PedidoFlow.Delay(pedido, Today, null, 3, Format).Text);
    }

    [Fact]
    public void In_planta_it_is_late_after_its_processing_days()
    {
        var pedido = new Pedido { Etapa = PedidoEtapa.EnPlanta };
        Assert.Equal(DelayLevel.None, PedidoFlow.Delay(pedido, Today, Today.AddDays(-2), 3, Format).Level);
        Assert.Equal(DelayLevel.Warning, PedidoFlow.Delay(pedido, Today, Today.AddDays(-3), 3, Format).Level);
        Assert.Equal(DelayLevel.Late, PedidoFlow.Delay(pedido, Today, Today.AddDays(-5), 3, Format).Level);
        // Listo and Entregado are never late for planta.
        Assert.Equal(DelayLevel.None, PedidoFlow.Delay(new Pedido { Etapa = PedidoEtapa.Listo }, Today, Today.AddDays(-9), 3, Format).Level);
    }
}
