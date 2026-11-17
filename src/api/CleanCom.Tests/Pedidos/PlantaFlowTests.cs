using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;

namespace CleanCom.Tests.Pedidos;

/// <summary>docs/03-reglas-de-negocio.md, Planta and Pedidos › Etapas (Recolectado → EnPlanta → Listo).</summary>
public class PlantaFlowTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 17);
    private static string Format(DateOnly d) => d.ToString("dd/MM");

    private static Ticket NewTicket(string folio) => new() { Folio = folio, Garments = 3 };

    [Fact]
    public void The_first_ticket_moves_the_pedido_to_en_planta()
    {
        var pedido = new Pedido { Id = 7, Etapa = PedidoEtapa.Recolectado };
        var evento = PlantaFlow.Receive(pedido, NewTicket("A1"), "Tintorería", Now);
        Assert.Equal(PedidoEtapa.EnPlanta, pedido.Etapa);
        Assert.Equal("Ticket A1: Tintorería, 3 prendas", evento.Note);

        var second = PlantaFlow.Receive(pedido, NewTicket("A2"), "Lavandería", Now);
        Assert.Equal(PedidoEventoKind.Ticket, second.Kind);
        Assert.Equal(PedidoEtapa.EnPlanta, pedido.Etapa);
    }

    [Theory]
    [InlineData(PedidoEtapa.Listo)]
    [InlineData(PedidoEtapa.Entregado)]
    [InlineData(PedidoEtapa.Cancelado)]
    public void A_pedido_already_ready_or_closed_does_not_receive_tickets(PedidoEtapa etapa)
    {
        var pedido = new Pedido { Id = 7, Etapa = etapa };
        Assert.Throws<DomainException>(() => PlantaFlow.Receive(pedido, NewTicket("A1"), "Tintorería", Now));
    }

    [Fact]
    public void The_last_ticket_ready_moves_the_pedido_to_listo()
    {
        var pedido = new Pedido { Etapa = PedidoEtapa.EnPlanta };
        var a = NewTicket("A1");
        var b = NewTicket("A2");
        Assert.False(PlantaFlow.MarkReady(a, pedido, [a, b], Now));
        Assert.Equal(PedidoEtapa.EnPlanta, pedido.Etapa);
        Assert.True(PlantaFlow.MarkReady(b, pedido, [a, b], Now));
        Assert.Equal(PedidoEtapa.Listo, pedido.Etapa);
        Assert.Equal(Now, b.ReadyAt);
        Assert.Contains("Todos los tickets listos (2)", pedido.Eventos.Last().Note);
    }

    [Fact]
    public void A_ticket_already_ready_gives_a_clear_error()
    {
        var pedido = new Pedido { Etapa = PedidoEtapa.EnPlanta };
        var a = NewTicket("A1");
        PlantaFlow.MarkReady(a, pedido, [a], Now);
        var error = Assert.Throws<DomainException>(() => PlantaFlow.MarkReady(a, pedido, [a], Now));
        Assert.Equal("El ticket A1 ya estaba listo.", error.Message);
    }

    [Fact]
    public void After_a_partial_entrega_the_last_ticket_does_not_take_the_pedido_back()
    {
        var pedido = new Pedido { Etapa = PedidoEtapa.EntregaConfirmada };
        var a = NewTicket("A1");
        Assert.False(PlantaFlow.MarkReady(a, pedido, [a], Now));
        Assert.Equal(PedidoEtapa.EntregaConfirmada, pedido.Etapa);
    }

    [Fact]
    public void Removing_the_only_ticket_in_process_leaves_the_rest_ready()
    {
        var pedido = new Pedido { Etapa = PedidoEtapa.EnPlanta };
        var a = NewTicket("A1");
        var wrong = NewTicket("A9");
        PlantaFlow.MarkReady(a, pedido, [a, wrong], Now);
        Assert.Throws<DomainException>(() => PlantaFlow.Deactivate(wrong, pedido, [a, wrong], " ", Now));
        Assert.True(PlantaFlow.Deactivate(wrong, pedido, [a, wrong], "Folio de otro pedido", Now));
        Assert.False(wrong.Active);
        Assert.Equal(PedidoEtapa.Listo, pedido.Etapa);
        Assert.Throws<DomainException>(() => PlantaFlow.Deactivate(a, pedido, [a], "x", Now));
    }

    [Fact]
    public void A_ticket_is_late_after_the_processing_days_of_its_linea()
    {
        var ticket = NewTicket("A1");
        Assert.Equal(DelayLevel.None, PlantaFlow.Delay(ticket, Today.AddDays(-2), Today, 3, Format).Level);
        Assert.Equal(DelayLevel.Warning, PlantaFlow.Delay(ticket, Today.AddDays(-3), Today, 3, Format).Level);
        var late = PlantaFlow.Delay(ticket, Today.AddDays(-5), Today, 3, Format);
        Assert.Equal("Atrasado 2 días · debía estar listo el 15/11", late.Text);

        ticket.State = TicketState.Listo;
        Assert.Equal(DelayLevel.None, PlantaFlow.Delay(ticket, Today.AddDays(-5), Today, 3, Format).Level);
    }
}
