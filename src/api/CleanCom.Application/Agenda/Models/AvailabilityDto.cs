namespace CleanCom.Application.Agenda;

/// <summary>A visit day of the zona and its occupancy ("mar 3 nov 8/14").</summary>
public record AgendaDayDto(DateOnly Date, string Label, int Occupied, int Cupo, bool Available);

/// <summary>
/// Answer of <c>POST /agenda/disponibilidad</c>: the "Así se agenda" panel.
/// </summary>
/// <param name="Date">First visit day with cupo; null when out of coverage or nothing was found.</param>
/// <param name="Place">Place the recolección takes that day (for example 9 of 14).</param>
/// <param name="Days">The chosen day followed by the next visit days, to change day with one click.</param>
/// <param name="Explanation">How the date was reached, step by step, in Spanish.</param>
public record AvailabilityDto(
    int? ZonaId,
    string? ZonaCode,
    int? RutaId,
    string? RutaCode,
    ZonaSource ZonaSource,
    bool OutOfCoverage,
    DateOnly? MinimumDate,
    DateOnly? Date,
    int? Place,
    int? Cupo,
    IReadOnlyList<AgendaDayDto> Days,
    IReadOnlyList<string> Explanation);
