namespace CleanCom.Application.Rutas;

/// <summary>Answer of <c>GET /rutas/dia</c>: every active ruta on a day.</summary>
/// <param name="Estimate">How times and km are estimated, in Spanish.</param>
public record RutasDiaDto(DateOnly Date, string Label, string? NonWorkingReason, string Estimate, IReadOnlyList<RutaPlanDto> Rutas);
