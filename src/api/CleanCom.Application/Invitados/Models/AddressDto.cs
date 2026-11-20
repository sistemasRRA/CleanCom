namespace CleanCom.Application.Invitados;

public record AddressDto(
    int Id,
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
    int? ZonaId,
    string? ZonaCode,
    bool ZonaAssignedManually,
    bool Active);
