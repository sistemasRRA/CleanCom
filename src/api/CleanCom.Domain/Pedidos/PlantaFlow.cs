using CleanCom.Domain.Common;

namespace CleanCom.Domain.Pedidos;

/// <summary>
/// Planta (docs/03-reglas-de-negocio.md, Planta and Etapas): the first ticket moves the pedido to EnPlanta; when all its
/// tickets are ready, the pedido goes to Listo. Etapas only move forward.
/// </summary>
public static class PlantaFlow
{
    /// <summary>
    /// Etapas that can receive tickets. A pedido already Listo or further is moved back to En planta by hand in Pedidos
    /// (with its motivo) before adding a ticket.
    /// </summary>
    public static readonly PedidoEtapa[] ReceivingEtapas = [PedidoEtapa.Agendado, PedidoEtapa.Recolectado, PedidoEtapa.EnPlanta];

    /// <summary>A ticket arrives for the pedido; the first one moves it to EnPlanta. Returns the history line.</summary>
    public static PedidoEvento Receive(Pedido pedido, Ticket ticket, string lineaName, DateTimeOffset now)
    {
        if (!ReceivingEtapas.Contains(pedido.Etapa))
        {
            throw new DomainException(pedido.Etapa == PedidoEtapa.Cancelado
                ? $"El pedido {pedido.Id} está cancelado."
                : $"El pedido {pedido.Id} ya está en {PedidoFlow.Name(pedido.Etapa)}. Para agregar un ticket, regrésalo a En planta en Pedidos.");
        }
        var line = $"Ticket {ticket.Folio}: {lineaName}, {ticket.Garments} prenda{(ticket.Garments == 1 ? "" : "s")}";
        return PedidoFlow.Advance(pedido, PedidoEtapa.EnPlanta, now, line) ?? Add(pedido, line);
    }

    /// <summary>
    /// Planta marks a ticket ready. When every active ticket of the pedido is ready, the pedido goes to Listo; returns
    /// true in that case.
    /// </summary>
    /// <param name="tickets">All tickets of the pedido, including <paramref name="ticket"/>.</param>
    public static bool MarkReady(Ticket ticket, Pedido pedido, IReadOnlyCollection<Ticket> tickets, DateTimeOffset now)
    {
        if (pedido.Etapa == PedidoEtapa.Cancelado)
        {
            throw new DomainException($"El ticket {ticket.Folio} es del pedido {pedido.Id}, que está cancelado.");
        }
        if (ticket.State == TicketState.Listo)
        {
            throw new DomainException($"El ticket {ticket.Folio} ya estaba listo.");
        }
        ticket.State = TicketState.Listo;
        ticket.ReadyAt = now;
        return CheckAllReady(pedido, tickets, now, $"Ticket {ticket.Folio} listo");
    }

    /// <summary>
    /// A ticket captured by mistake is given de baja (only while in process). If the rest are ready, the pedido goes to
    /// Listo; returns true in that case.
    /// </summary>
    public static bool Deactivate(Ticket ticket, Pedido pedido, IReadOnlyCollection<Ticket> tickets, string reason, DateTimeOffset now)
    {
        if (ticket.State == TicketState.Listo)
        {
            throw new DomainException($"El ticket {ticket.Folio} ya está listo; no se puede dar de baja.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Para dar de baja el ticket escribe el motivo.");
        }
        ticket.Active = false;
        return CheckAllReady(pedido, tickets, now, $"Ticket {ticket.Folio} dado de baja: {reason.Trim()}");
    }

    /// <summary>
    /// Atraso of a ticket: against the processing days of its línea, counted from the day it arrived. Due today is
    /// yellow; a ready ticket is never late.
    /// </summary>
    public static PedidoDelay Delay(Ticket ticket, DateOnly received, DateOnly today, int processingDays, Func<DateOnly, string> format)
    {
        if (ticket.State == TicketState.Listo)
        {
            return PedidoDelay.OnTime;
        }
        var due = received.AddDays(processingDays);
        var days = today.DayNumber - due.DayNumber;
        return days switch
        {
            > 0 => new PedidoDelay(DelayLevel.Late, $"Atrasado {days} día{(days == 1 ? "" : "s")} · debía estar listo el {format(due)}"),
            0 => new PedidoDelay(DelayLevel.Warning, "Hoy debe quedar listo"),
            _ => PedidoDelay.OnTime,
        };
    }

    private static bool CheckAllReady(Pedido pedido, IReadOnlyCollection<Ticket> tickets, DateTimeOffset now, string line)
    {
        var active = tickets.Where(t => t.Active).ToList();
        if (active.Count > 0 && active.All(t => t.State == TicketState.Listo)
            && PedidoFlow.Advance(pedido, PedidoEtapa.Listo, now, $"{line}. Todos los tickets listos ({active.Count}).") is not null)
        {
            return true;
        }
        Add(pedido, line);
        return false;
    }

    private static PedidoEvento Add(Pedido pedido, string line)
    {
        var evento = new PedidoEvento { PedidoId = pedido.Id, Kind = PedidoEventoKind.Ticket, Automatic = true, Note = line };
        pedido.Eventos.Add(evento);
        return evento;
    }
}
