using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Domain.Agenda;

/// <summary>
/// The invitado app uses the same agenda engine as the office, with the Invitado role's rules
/// (docs/03-reglas-de-negocio.md, Agenda): no specific time nor priority, the hora de registro is always now and the
/// zona is located from the address (only the office chooses it by hand).
/// </summary>
public static class InvitadoScheduling
{
    /// <summary>Throws, in Spanish, when the app sends something only the office can set.</summary>
    public static void EnsureAllowed(Turno turno, TimeOnly? specificTime, bool priority, DateTimeOffset? registeredAt, int? zonaId)
    {
        if (turno == Turno.HoraEspecifica || specificTime is not null)
        {
            throw new DomainException("La hora específica solo la puede poner oficina. Elige mañana, tarde o cualquier hora.");
        }
        if (priority)
        {
            throw new DomainException("La prioridad solo la puede poner oficina.");
        }
        if (registeredAt is not null)
        {
            throw new DomainException("La hora de registro es la de ahora; solo oficina puede cambiarla.");
        }
        if (zonaId is not null)
        {
            throw new DomainException("La zona sale de tu dirección; solo oficina puede elegirla a mano.");
        }
    }

    /// <summary>
    /// Changing the day or cancelling from the app is only for a recolección that is still to be done: the pedido is
    /// Agendado and the chofer has not left for it (its parada is pendiente, or there is none).
    /// </summary>
    public static bool IsChangeable(PedidoEtapa etapa, ParadaState? recoleccionState) =>
        etapa == PedidoEtapa.Agendado && recoleccionState is null or ParadaState.Pendiente;

    /// <summary>Throws, in Spanish, when the invitado cannot move the recolección to another day.</summary>
    public static void EnsureCanChangeDay(PedidoEtapa etapa, ParadaState? recoleccionState)
    {
        if (!IsChangeable(etapa, recoleccionState))
        {
            throw new DomainException(NotChangeable(etapa));
        }
    }

    /// <summary>Whether the invitado can still cancel: a changeable recolección, up to Agenda.CorteCancelacion of the day before.</summary>
    public static bool CanCancel(PedidoEtapa etapa, ParadaState? recoleccionState, DateTimeOffset now, DateOnly recoleccionDate, TimeOnly corteCancelacion) =>
        IsChangeable(etapa, recoleccionState) && AgendaCutoffs.CanCancelFromApp(now, recoleccionDate, corteCancelacion);

    /// <summary>Throws, in Spanish, when the invitado cannot cancel from the app; after the cutoff only the office can.</summary>
    public static void EnsureCanCancel(PedidoEtapa etapa, ParadaState? recoleccionState, DateTimeOffset now, DateOnly recoleccionDate, TimeOnly corteCancelacion)
    {
        if (!IsChangeable(etapa, recoleccionState))
        {
            throw new DomainException(NotChangeable(etapa));
        }
        if (!AgendaCutoffs.CanCancelFromApp(now, recoleccionDate, corteCancelacion))
        {
            throw new DomainException(
                $"Se puede cancelar desde la app hasta las {corteCancelacion:HH\\:mm} del día anterior a la recolección. Comunícate con oficina para cancelarlo.");
        }
    }

    private static string NotChangeable(PedidoEtapa etapa) => etapa switch
    {
        PedidoEtapa.Cancelado => "El pedido ya está cancelado.",
        PedidoEtapa.Agendado => "Tu chofer ya va en camino; comunícate con oficina para cualquier cambio.",
        _ => "Ya recogimos tus prendas; comunícate con oficina para cualquier cambio.",
    };
}
