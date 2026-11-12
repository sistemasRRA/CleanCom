namespace CleanCom.Domain.Agenda;

/// <summary>
/// Visit day of the zona with its occupancy, as shown in "Así se agenda" (for example "mar 3 nov 8/14").
/// </summary>
/// <param name="Occupied">Paradas already taking cupo that day.</param>
/// <param name="Available">Whether a new recolección still fits.</param>
public record AgendaDay(DateOnly Date, int Occupied, int Cupo, bool Available);

/// <summary>
/// Answer of the agenda engine.
/// </summary>
/// <param name="Date">First visit day with cupo, or null when none was found within the search limit.</param>
/// <param name="Place">Place the new recolección takes that day (occupied + 1).</param>
/// <param name="Days">The chosen day followed by the next visit days, with their occupancy. Days off are left out.</param>
/// <param name="Explanation">Steps in Spanish explaining how the date was reached.</param>
public record AvailabilityResult(
    DateOnly? Date,
    int? Place,
    int Cupo,
    IReadOnlyList<AgendaDay> Days,
    IReadOnlyList<string> Explanation);
