namespace CleanCom.Domain.Agenda;

/// <summary>
/// What the agenda engine needs to find a date, already loaded and resolved by the caller.
/// </summary>
/// <param name="MinimumDate">First date allowed by the cutoffs (<see cref="AgendaCutoffs"/>).</param>
/// <param name="VisitDays">Days the ruta comes by the zona (the zona núcleo's for an EXT zona).</param>
/// <param name="NonWorkingDays">Days off in the searched range.</param>
/// <param name="Occupancy">Paradas that take cupo per date (fixed paradas included). Missing dates count as 0.</param>
/// <param name="CupoParadas">Ruta.CupoParadas that applies to the ruta.</param>
/// <param name="OtherDaysCount">How many visit days to list besides the chosen one.</param>
/// <param name="SearchDays">Safety limit: how many calendar days to search before giving up.</param>
public record AvailabilityQuery(
    DateOnly MinimumDate,
    IReadOnlyCollection<DayOfWeek> VisitDays,
    IReadOnlySet<DateOnly> NonWorkingDays,
    IReadOnlyDictionary<DateOnly, int> Occupancy,
    int CupoParadas,
    int OtherDaysCount = 6,
    int SearchDays = 90);
