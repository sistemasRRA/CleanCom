using System.Globalization;
using CleanCom.Domain.Common;

namespace CleanCom.Domain.Routing;

/// <summary>
/// Inicio and fin de turno in the chofer app (docs/pantallas/apps/chofer.md, Flujo del turno, steps 1 and 7): the
/// odometer readings. The unidad's km never goes down, and a shift longer than Unidad.KmMaximoPorTurno asks the
/// chofer to confirm, so a typo (one digit more) is caught before it reaches the unidad.
/// </summary>
public static class ShiftRules
{
    /// <summary>
    /// Checks the starting reading against the unidad's last known km. Throws when it is lower; returns a warning, in
    /// Spanish, when it jumped more than <paramref name="maxKmPerShift"/> (null when it looks right). A unidad without
    /// km yet (0) accepts anything.
    /// </summary>
    public static string? CheckStart(int km, int unidadKm, string plate, int maxKmPerShift)
    {
        if (km < 0)
        {
            throw new DomainException("El kilometraje no puede ser negativo.");
        }
        if (km < unidadKm)
        {
            throw new DomainException(
                $"El kilometraje no puede ser menor al último de la unidad {plate} ({Km(unidadKm)} km). Revisa el tablero; si está bien, avisa a oficina.");
        }
        return unidadKm > 0 && km - unidadKm > maxKmPerShift
            ? $"Son {Km(km - unidadKm)} km más que el último registro de la unidad {plate} ({Km(unidadKm)} km)."
            : null;
    }

    /// <summary>
    /// Checks the final reading against the starting one. Throws when it is lower; returns what the chofer should
    /// confirm, in Spanish: a shift longer than <paramref name="maxKmPerShift"/> and paradas still open.
    /// </summary>
    public static IReadOnlyList<string> CheckEnd(int km, int startKm, int maxKmPerShift, int openParadas)
    {
        if (km < startKm)
        {
            throw new DomainException($"El kilometraje final no puede ser menor al inicial ({Km(startKm)} km).");
        }
        var warnings = new List<string>();
        if (km - startKm > maxKmPerShift)
        {
            warnings.Add($"Recorriste {Km(km - startKm)} km en el turno (de {Km(startKm)} a {Km(km)}).");
        }
        if (openParadas > 0)
        {
            warnings.Add(openParadas == 1 ? "Te queda 1 parada sin cerrar; oficina la verá." : $"Te quedan {openParadas} paradas sin cerrar; oficina las verá.");
        }
        return warnings;
    }

    /// <summary>
    /// The shift is required: the chofer works on today's paradas (en camino, llegué, fotos, firma, cerrar) only
    /// between "iniciar turno" and "cerrar turno". An action of another day that syncs late (sin señal) is not checked.
    /// </summary>
    public static void EnsureOpen(DateTimeOffset? startedAt, DateTimeOffset? endedAt, bool isToday)
    {
        if (!isToday)
        {
            return;
        }
        if (startedAt is null)
        {
            throw new DomainException("Inicia tu turno antes de salir a tus paradas.");
        }
        if (endedAt is not null)
        {
            throw new DomainException("Ya cerraste tu turno. Si te falta una parada, avisa a oficina.");
        }
    }

    private static readonly CultureInfo Spanish = new("es-MX");

    /// <summary>"45,200": km with thousands separator, the same on any server.</summary>
    public static string Km(int km) => km.ToString("N0", Spanish);
}
