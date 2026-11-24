namespace CleanCom.Domain.Routing;

/// <summary>
/// What the invitado app shows of the chofer coming to their address (docs/pantallas/apps/invitado.md, Seguimiento).
/// </summary>
public static class InvitadoTracking
{
    /// <summary>The chofer's position is only shown while the invitado's parada is "en camino".</summary>
    public static bool ShowsChofer(ParadaState? visitState) => visitState == ParadaState.EnCamino;

    /// <summary>
    /// When the chofer should arrive: from the time of their last position, the straight line to the address times
    /// Ruta.FactorDistancia at Ruta.VelocidadPromedio, the same estimate the ruta plan uses until Google is connected.
    /// </summary>
    public static DateTimeOffset EstimatedArrival(GeoPoint chofer, DateTimeOffset readingAt, GeoPoint destination, double speedKmh, double distanceFactor)
    {
        var km = RoutePlanner.Km(chofer, destination) * distanceFactor;
        return readingAt.AddMinutes(Math.Ceiling(km / speedKmh * 60));
    }

    /// <summary>Only the first name of the chofer goes to the invitado.</summary>
    public static string? FirstName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
}
