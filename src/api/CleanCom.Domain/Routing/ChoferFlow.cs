using CleanCom.Domain.Common;

namespace CleanCom.Domain.Routing;

/// <summary>A parada of the chofer's ruta, in the assigned order, as the flow rules see it.</summary>
public readonly record struct FlowParada(int Id, int Order, string Name, ParadaState State);

/// <summary>
/// "En camino" and "Llegué" (docs/pantallas/apps/chofer.md, Flujo del turno, steps 3 and 4).
/// Visita.OrdenObligatorio: the paradas are taken in the assigned order. One "en camino" at a time.
/// Llegué is checked against Gps.ToleranciaMetros.
/// </summary>
public static class ChoferFlow
{
    /// <summary>The parada the chofer should go to next: the first one still pending, in order.</summary>
    public static FlowParada? Next(IReadOnlyList<FlowParada> ordered) =>
        ordered.Where(p => p.State == ParadaState.Pendiente).Cast<FlowParada?>().FirstOrDefault();

    /// <summary>Throws with the reason, in Spanish, when the chofer cannot go "en camino" to <paramref name="target"/>.</summary>
    public static void EnsureCanStart(FlowParada target, IReadOnlyList<FlowParada> ordered, bool orderRequired)
    {
        if (target.State != ParadaState.Pendiente)
        {
            throw new DomainException(target.State switch
            {
                ParadaState.EnCamino => "Ya vas en camino a esta parada.",
                ParadaState.EnSitio => "Ya registraste tu llegada a esta parada.",
                _ => "Esta parada ya se cerró.",
            });
        }
        if (ordered.FirstOrDefault(p => p.State == ParadaState.EnCamino && p.Id != target.Id) is { Id: > 0 } current)
        {
            throw new DomainException($"Ya vas en camino a la parada {current.Order} ({current.Name}); marca Llegué primero.");
        }
        EnsureOrder(target, ordered, orderRequired);
    }

    /// <summary>Throws when the chofer cannot mark "llegué" at <paramref name="target"/>.</summary>
    public static void EnsureCanArrive(FlowParada target, IReadOnlyList<FlowParada> ordered, bool orderRequired)
    {
        switch (target.State)
        {
            case ParadaState.EnCamino:
                return;
            case ParadaState.Pendiente:
                // Arriving without having marked "en camino" is allowed, but in order and with nothing else en camino.
                EnsureCanStart(target, ordered, orderRequired);
                return;
            case ParadaState.EnSitio:
                throw new DomainException("Ya registraste tu llegada a esta parada.");
            default:
                throw new DomainException("Esta parada ya se cerró.");
        }
    }

    /// <summary>
    /// Whether the arrival is within the tolerance. Unknown distance (no GPS or parada without coordinates) is accepted
    /// and shown to the office as such.
    /// </summary>
    public static bool WithinTolerance(int? distanceMeters, int toleranceMeters) =>
        distanceMeters is null || distanceMeters <= toleranceMeters;

    /// <summary>
    /// When an action happened: the phone's time (it may sync later, sin señal), never in the future nor before the
    /// start of the day of the visit; otherwise now.
    /// </summary>
    public static DateTimeOffset ActionTime(DateTimeOffset? at, DateTimeOffset now, DateTimeOffset dayStart) =>
        at is { } t && t <= now && t >= dayStart ? t : now;

    public static int DistanceMeters(GeoPoint a, GeoPoint b) => (int)Math.Round(RoutePlanner.Km(a, b) * 1000);

    private static void EnsureOrder(FlowParada target, IReadOnlyList<FlowParada> ordered, bool orderRequired)
    {
        if (orderRequired && Next(ordered) is { } next && next.Id != target.Id)
        {
            throw new DomainException($"Las paradas se visitan en el orden asignado: sigue la {next.Order} ({next.Name}). Si necesitas cambiarlo, pídelo a oficina.");
        }
    }
}
