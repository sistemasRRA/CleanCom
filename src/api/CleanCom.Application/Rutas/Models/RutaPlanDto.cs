namespace CleanCom.Application.Rutas;

/// <summary>One ruta on one day: chofer, paradas in order with estimated times, km, return and alerts.</summary>
/// <param name="RutaDiaId">Null while nothing is scheduled for the ruta that day.</param>
/// <param name="OperadorIsUsual">No chofer was assigned for the day: it is the operador whose usual ruta this is.</param>
/// <param name="Optimized">Every parada has a position; false when new ones arrived after optimizing.</param>
/// <param name="Alerts">Before saving (docs/03-reglas-de-negocio.md, Rutas, rule 6), in Spanish.</param>
public record RutaPlanDto(
    int RutaId,
    string Code,
    string Name,
    string Color,
    int? RutaDiaId,
    int? OperadorId,
    string? OperadorName,
    bool OperadorIsUsual,
    int Occupied,
    int Cupo,
    bool Optimized,
    TimeOnly Departure,
    TimeOnly? LunchStart,
    TimeOnly ReturnAt,
    TimeOnly MaxReturn,
    double Km,
    int Garments,
    int Capacity,
    IReadOnlyList<string> Alerts,
    IReadOnlyList<RutaPlanParadaDto> Paradas,
    int? UnidadId = null,
    string? UnidadPlate = null);
