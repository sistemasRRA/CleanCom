namespace CleanCom.Domain.Crm;

/// <summary>docs/03-reglas-de-negocio.md, CRM › Frecuencia y estatus.</summary>
public enum CrmStatus
{
    Prospecto,
    Nuevo,
    Activo,
    LeToca,
    EnRiesgo,
    Inactivo,
}

/// <summary>Thresholds from the Operación settings (Crm.DiasNuevo, Crm.DiasInactivo, Crm.FactorEnRiesgo).</summary>
public record CrmThresholds(int NewDays, int InactiveDays, decimal AtRiskFactor);

/// <param name="FrequencyDays">The office's fixed frequency, or the median of days between services; null with fewer than 2 services.</param>
/// <param name="LeTocaDate">Last service plus frequency.</param>
/// <param name="Why">The reason in Spanish, for example "Pide cada 7 días; su último servicio fue hace 9."</param>
public record CrmEvaluation(
    CrmStatus Status,
    int ServiceCount,
    DateOnly? FirstService,
    DateOnly? LastService,
    int? DaysSinceLast,
    int? FrequencyDays,
    DateOnly? LeTocaDate,
    string Why);

/// <summary>
/// The CRM status of an invitado from their service dates. Pure: the nightly job and the screens use the same rule.
/// </summary>
public static class CrmRules
{
    /// <param name="serviceDates">Dates of their services (past recolecciones); repeated dates count once.</param>
    /// <param name="nextVisit">Next day their zona's ruta comes by, after today; null when the zona is unknown.</param>
    /// <param name="fixedFrequencyDays">Frequency set by hand by the office; wins over the calculated one.</param>
    public static CrmEvaluation Evaluate(
        IEnumerable<DateOnly> serviceDates,
        DateOnly today,
        DateOnly? nextVisit,
        CrmThresholds t,
        int? fixedFrequencyDays = null)
    {
        var dates = serviceDates.Where(d => d <= today).Distinct().Order().ToList();
        if (dates.Count == 0)
        {
            return new CrmEvaluation(CrmStatus.Prospecto, 0, null, null, null, fixedFrequencyDays, null, "Todavía no tiene servicios.");
        }

        var first = dates[0];
        var last = dates[^1];
        var since = today.DayNumber - last.DayNumber;
        var frequency = fixedFrequencyDays ?? MedianGap(dates);
        DateOnly? leToca = frequency is null ? null : last.AddDays(frequency.Value);
        var lastText = since == 0 ? "su último servicio fue hoy" : since == 1 ? "su último servicio fue ayer" : $"su último servicio fue hace {since} días";
        var pattern = frequency is null ? "Tiene un servicio" : $"Pide cada {frequency} días";

        CrmEvaluation Result(CrmStatus status, string why) =>
            new(status, dates.Count, first, last, since, frequency, leToca, why);

        if (since > t.InactiveDays)
        {
            return Result(CrmStatus.Inactivo, $"{Capitalize(lastText)}; más de {t.InactiveDays} días sin pedir.");
        }
        if (frequency is not null && since >= (double)t.AtRiskFactor * frequency.Value)
        {
            return Result(CrmStatus.EnRiesgo, $"{pattern}; {lastText} ({t.AtRiskFactor:0.#} veces su frecuencia).");
        }
        if (leToca is not null && leToca <= (nextVisit ?? today))
        {
            return Result(CrmStatus.LeToca, $"{pattern}; {lastText}. Le toca el {leToca:dd/MM}.");
        }
        if (today.DayNumber - first.DayNumber <= t.NewDays)
        {
            return Result(CrmStatus.Nuevo, $"Su primer servicio fue hace {today.DayNumber - first.DayNumber} días.");
        }
        return Result(CrmStatus.Activo, $"{pattern}; {lastText}.");
    }

    /// <summary>Median of the days between consecutive services; null with fewer than 2 services.</summary>
    public static int? MedianGap(IReadOnlyList<DateOnly> orderedDates)
    {
        if (orderedDates.Count < 2)
        {
            return null;
        }
        var gaps = orderedDates.Zip(orderedDates.Skip(1), (a, b) => b.DayNumber - a.DayNumber).Order().ToList();
        var middle = gaps.Count / 2;
        var median = gaps.Count % 2 == 1 ? gaps[middle] : (gaps[middle - 1] + gaps[middle]) / 2.0;
        return Math.Max(1, (int)Math.Round(median, MidpointRounding.AwayFromZero));
    }

    /// <summary>First day on or after <paramref name="from"/> that is a visit day.</summary>
    public static DateOnly? NextVisitDay(DateOnly from, IReadOnlyCollection<DayOfWeek> visitDays)
    {
        if (visitDays.Count == 0)
        {
            return null;
        }
        for (var d = from; ; d = d.AddDays(1))
        {
            if (visitDays.Contains(d.DayOfWeek))
            {
                return d;
            }
        }
    }

    private static string Capitalize(string s) => char.ToUpperInvariant(s[0]) + s[1..];
}
