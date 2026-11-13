namespace CleanCom.Application.Invitados;

/// <param name="Lat">Coordinates from Google Places; they decide the zona.</param>
/// <param name="ZonaId">Zona chosen by hand by the office (ZonaAsignadaManual). Empty = located automatically.</param>
public record SaveDireccionRequest(
    string? Label,
    string Street,
    string? ExteriorNumber,
    string? InteriorNumber,
    string PostalCode,
    string? Neighborhood,
    string? Borough,
    string? References,
    double? Lat,
    double? Lng,
    int? ZonaId);
