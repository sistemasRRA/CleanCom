using CleanCom.Domain.Geography;

namespace CleanCom.Application.Catalogs;

public record ZonaDto(
    int Id,
    string Code,
    int RutaId,
    string RutaCode,
    int? ZonaNucleoId,
    string? ZonaNucleoCode,
    bool Active,
    IReadOnlyList<DayOfWeek> VisitDays,
    IReadOnlyList<DayOfWeek> EffectiveVisitDays,
    IReadOnlyList<Vertex>? Polygon);
