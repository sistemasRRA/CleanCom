using CleanCom.Domain.Pedidos;

namespace CleanCom.Domain.Routing;

/// <summary>
/// Order of the paradas until Google optimizes the ruta (week 3): morning first, then the afternoon, and "any time"
/// last so it fills the gaps. A specific time goes with the turno it falls in, by hour. The end of the morning comes
/// from the Turno.Manana setting.
/// </summary>
public static class ProvisionalOrder
{
    public static int Rank(Turno turno, TimeOnly? specificTime, TimeOnly morningEnd) => turno switch
    {
        Turno.Manana => 0,
        Turno.HoraEspecifica => specificTime is { } t && t < morningEnd ? 0 : 1,
        Turno.Tarde => 1,
        _ => 2,
    };
}
