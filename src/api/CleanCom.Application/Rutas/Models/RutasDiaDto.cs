namespace CleanCom.Application.Rutas;

/// <summary>Answer of <c>GET /rutas/dia</c>: every active ruta on a day.</summary>
/// <param name="Estimate">How times and km are estimated, in Spanish.</param>
/// <param name="BaseLat">The base (Leones, setting Base.Ubicacion), where every ruta starts and ends: for the map.</param>
public record RutasDiaDto(
    DateOnly Date,
    string Label,
    string? NonWorkingReason,
    string Estimate,
    IReadOnlyList<RutaPlanDto> Rutas,
    double BaseLat,
    double BaseLng);
