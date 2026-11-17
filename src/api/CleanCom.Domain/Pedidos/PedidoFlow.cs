using CleanCom.Domain.Common;

namespace CleanCom.Domain.Pedidos;

/// <summary>What happened to a pedido; each one is a line of its history.</summary>
public enum PedidoEventoKind
{
    /// <summary>Changed etapa (by an event of the chofer or planta, or by hand).</summary>
    Etapa,
    /// <summary>Office scheduled or changed the entrega date.</summary>
    EntregaProgramada,
    /// <summary>A failed entrega attempt; the pedido was rescheduled.</summary>
    NoEntregado,
    /// <summary>Comment from the office or planta.</summary>
    Comentario,
}

/// <summary>
/// History of a pedido (docs/03-reglas-de-negocio.md, Pedidos). Who and when are <see cref="Entity.CreatedBy"/> and
/// <see cref="Entity.CreatedAt"/>. Never deleted.
/// </summary>
public class PedidoEvento : Entity
{
    public int PedidoId { get; set; }
    public PedidoEventoKind Kind { get; set; }
    public PedidoEtapa? FromEtapa { get; set; }
    public PedidoEtapa? ToEtapa { get; set; }
    /// <summary>Recolección or entrega date involved (new entrega date, rescheduled date).</summary>
    public DateOnly? Date { get; set; }
    /// <summary>Done by the system (chofer app, planta, POS) rather than moved by hand.</summary>
    public bool Automatic { get; set; }
    public int? ReasonId { get; set; }
    /// <summary>Motivo or comment as written.</summary>
    public string? Note { get; set; }
}

public enum DelayLevel
{
    None,
    /// <summary>Due today: the traffic light turns yellow before it is late.</summary>
    Warning,
    Late,
}

/// <param name="Text">"Atrasado 6 días · tocaba el mar 27 oct", in Spanish; null when on time.</param>
public readonly record struct PedidoDelay(DelayLevel Level, string? Text)
{
    public static readonly PedidoDelay OnTime = new(DelayLevel.None, null);
}

/// <summary>
/// Etapas of a pedido: Agendado → Recolectado → EnPlanta → Listo → EntregaConfirmada → EnCamino → Entregado, and
/// Cancelado (docs/03-reglas-de-negocio.md, Pedidos › Etapas).
/// </summary>
public static class PedidoFlow
{
    public static readonly PedidoEtapa[] Order =
    [
        PedidoEtapa.Agendado, PedidoEtapa.Recolectado, PedidoEtapa.EnPlanta, PedidoEtapa.Listo,
        PedidoEtapa.EntregaConfirmada, PedidoEtapa.EnCamino, PedidoEtapa.Entregado,
    ];

    public static string Name(PedidoEtapa etapa) => etapa switch
    {
        PedidoEtapa.Agendado => "Agendado",
        PedidoEtapa.Recolectado => "Recolectado",
        PedidoEtapa.EnPlanta => "En planta",
        PedidoEtapa.Listo => "Listo",
        PedidoEtapa.EntregaConfirmada => "Entrega confirmada",
        PedidoEtapa.EnCamino => "En camino",
        PedidoEtapa.Entregado => "Entregado",
        _ => "Cancelado",
    };

    /// <summary>
    /// A move by hand. Forward is free; going back, delivering by hand, cancelling or leaving Cancelado needs a motivo
    /// (it stays in the history).
    /// </summary>
    public static bool NeedsReason(PedidoEtapa from, PedidoEtapa to) =>
        to == PedidoEtapa.Entregado
        || to == PedidoEtapa.Cancelado
        || from == PedidoEtapa.Cancelado
        || Array.IndexOf(Order, to) < Array.IndexOf(Order, from);

    /// <summary>Checks a move by hand and returns the history line to save.</summary>
    public static PedidoEvento Move(Pedido pedido, PedidoEtapa to, string? reason, DateTimeOffset now)
    {
        if (pedido.Etapa == to)
        {
            throw new DomainException($"El pedido ya está en {Name(to)}.");
        }
        if (NeedsReason(pedido.Etapa, to) && string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(to switch
            {
                PedidoEtapa.Entregado => "Para marcar como entregado a mano escribe el motivo.",
                PedidoEtapa.Cancelado => "Para cancelar el pedido escribe el motivo.",
                _ => $"Para regresar el pedido a {Name(to)} escribe el motivo.",
            });
        }
        return Apply(pedido, to, automatic: false, reason, now);
    }

    /// <summary>
    /// A move triggered by an event (chofer closes a recolección, first ticket, all tickets ready…). Only forward:
    /// an event that arrives late never takes a pedido back. Returns null when nothing changes.
    /// </summary>
    public static PedidoEvento? Advance(Pedido pedido, PedidoEtapa to, DateTimeOffset now, string? note = null)
    {
        if (pedido.Etapa == PedidoEtapa.Cancelado || Array.IndexOf(Order, to) <= Array.IndexOf(Order, pedido.Etapa))
        {
            return null;
        }
        return Apply(pedido, to, automatic: true, note, now);
    }

    private static PedidoEvento Apply(Pedido pedido, PedidoEtapa to, bool automatic, string? note, DateTimeOffset now)
    {
        var evento = new PedidoEvento
        {
            PedidoId = pedido.Id,
            Kind = PedidoEventoKind.Etapa,
            FromEtapa = pedido.Etapa,
            ToEtapa = to,
            Automatic = automatic,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };
        pedido.Etapa = to;
        pedido.EtapaSince = now;
        pedido.Eventos.Add(evento);
        return evento;
    }

    /// <summary>
    /// Atraso (Pedidos › Atraso): recolección or entrega date already passed, or more days in planta than promised.
    /// Due today is a yellow light. The days allowed per etapa are still to be confirmed (docs/09-pendientes.md).
    /// </summary>
    /// <param name="plantaSince">Business date it entered planta (EnPlanta), if it did.</param>
    /// <param name="processingDays">Promised days of its líneas de servicio (the longest), or Planta.DiasProceso.</param>
    public static PedidoDelay Delay(Pedido pedido, DateOnly today, DateOnly? plantaSince, int processingDays, Func<DateOnly, string> format)
    {
        switch (pedido.Etapa)
        {
            case PedidoEtapa.Agendado:
                return ByDate(pedido.RecoleccionDate, "la recolección");
            case PedidoEtapa.EntregaConfirmada or PedidoEtapa.EnCamino when pedido.EntregaDate is { } entrega:
                return ByDate(entrega, "la entrega");
            case PedidoEtapa.Recolectado or PedidoEtapa.EnPlanta when plantaSince is { } since:
                var due = since.AddDays(processingDays);
                return ByDate(due, "tenerlo listo");
            default:
                return PedidoDelay.OnTime;
        }

        PedidoDelay ByDate(DateOnly due, string what)
        {
            var days = today.DayNumber - due.DayNumber;
            return days switch
            {
                > 0 => new PedidoDelay(DelayLevel.Late, $"Atrasado {days} día{(days == 1 ? "" : "s")} · tocaba {what} el {format(due)}"),
                0 => new PedidoDelay(DelayLevel.Warning, $"Hoy toca {what}"),
                _ => PedidoDelay.OnTime,
            };
        }
    }
}
