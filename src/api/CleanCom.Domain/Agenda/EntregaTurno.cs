using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;

namespace CleanCom.Domain.Agenda;

/// <summary>
/// Turno of an entrega (Pedidos › Programar entrega): mañana, tarde or cualquier hora, chosen by the office with the
/// invitado. An entrega has no specific time.
/// </summary>
public static class EntregaTurno
{
    public static void EnsureAllowed(Turno? turno)
    {
        if (turno == Turno.HoraEspecifica)
        {
            throw new DomainException("La entrega no lleva hora específica; elige mañana, tarde o cualquier hora.");
        }
    }

    /// <summary>
    /// When the office does not choose one: the turno of its latest entrega (a new date or a new attempt keeps what was
    /// agreed), otherwise the recolección's. A recolección at a specific time gives cualquier hora.
    /// </summary>
    public static Turno Default(Turno? lastEntrega, Turno recoleccion)
    {
        var turno = lastEntrega ?? recoleccion;
        return turno == Turno.HoraEspecifica ? Turno.CualquierHora : turno;
    }
}
