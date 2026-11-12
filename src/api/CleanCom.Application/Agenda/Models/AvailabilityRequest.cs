using CleanCom.Domain.Pedidos;

namespace CleanCom.Application.Agenda;

/// <summary>
/// Where and when to look for a date. Either a saved address (<paramref name="DireccionId"/>) or the address being
/// captured (coordinates from Google and/or postal code).
/// </summary>
/// <param name="ZonaId">Zona chosen by hand by the office; skips locating it.</param>
/// <param name="RegisteredAt">Hora de registro. Empty = now. The office may set an earlier one (a call before the cutoff).</param>
public record AvailabilityRequest(
    int? DireccionId,
    double? Lat,
    double? Lng,
    string? PostalCode,
    string? Neighborhood,
    int? ZonaId,
    Turno Turno = Turno.CualquierHora,
    DateTimeOffset? RegisteredAt = null);
