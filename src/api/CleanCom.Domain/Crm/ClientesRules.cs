using System.Globalization;
using CleanCom.Domain.Kpis;

namespace CleanCom.Domain.Crm;

/// <summary>The period shown on Clientes y fuentes.</summary>
public enum ClientesPeriod
{
    Semana,
    Mes,
    Trimestre,
    Anio,
}

/// <summary>How an invitado attended in a period counts (each one is exactly one).</summary>
public enum AttendedKind
{
    /// <summary>Their first service is in the period.</summary>
    Nuevo,
    /// <summary>Their service before the first one of the period was at most Crm.DiasInactivo days earlier.</summary>
    Recurrente,
    /// <summary>They came back after more than Crm.DiasInactivo days without a service.</summary>
    Recuperado,
}

/// <summary>
/// Clientes y fuentes (docs/03-reglas-de-negocio.md, CRM › Clientes y fuentes). A service is a pedido not cancelled,
/// on its recolección date (the same the CRM uses). The 120 days are Crm.DiasInactivo, shared with the CRM: more than
/// that without a service is inactive (perdido).
/// </summary>
public static class ClientesRules
{
    private static readonly CultureInfo Spanish = new("es-MX");

    /// <summary>The period that contains <paramref name="date"/>; weeks from Monday to Sunday.</summary>
    public static DateRange Range(ClientesPeriod period, DateOnly date) => period switch
    {
        ClientesPeriod.Anio => new DateRange(new DateOnly(date.Year, 1, 1), new DateOnly(date.Year, 12, 31)),
        _ => KpiRules.Range((KpiPeriod)period, date),
    };

    /// <summary>The period right before the one that contains <paramref name="date"/>.</summary>
    public static DateRange Previous(ClientesPeriod period, DateOnly date) => Range(period, Range(period, date).Start.AddDays(-1));

    public static string Label(ClientesPeriod period, DateRange range) =>
        period == ClientesPeriod.Anio ? range.Start.Year.ToString(CultureInfo.InvariantCulture) : KpiRules.Label((KpiPeriod)period, range);

    /// <summary>
    /// What is compared with the period: the same number of days from the start of the previous period while the
    /// period is open (partial), the whole previous period otherwise. Example: November 1 to 26 against October 1 to 26.
    /// </summary>
    public static DateRange Comparison(ClientesPeriod period, DateRange range, DateRange elapsed)
    {
        var previous = Previous(period, range.Start);
        if (elapsed == range)
        {
            return previous;
        }
        var end = previous.Start.AddDays(elapsed.Days - 1);
        return new DateRange(previous.Start, end < previous.End ? end : previous.End);
    }

    /// <summary>How the invitado counts in the period, or null when they had no service in it.</summary>
    /// <param name="services">The invitado's service dates, in order.</param>
    public static AttendedKind? Attended(IReadOnlyList<DateOnly> services, DateRange range, int inactiveDays)
    {
        var index = -1;
        for (var i = 0; i < services.Count; i++)
        {
            if (range.Contains(services[i]))
            {
                index = i;
                break;
            }
        }
        if (index < 0)
        {
            return null;
        }
        if (index == 0)
        {
            return AttendedKind.Nuevo;
        }
        var gap = services[index].DayNumber - services[index - 1].DayNumber;
        return gap > inactiveDays ? AttendedKind.Recuperado : AttendedKind.Recurrente;
    }

    /// <summary>
    /// The day in the period the invitado became perdido (more than <paramref name="inactiveDays"/> days without a
    /// service), up to <paramref name="today"/>; null when it did not happen. Example: last service on July 1 with 120
    /// days → perdido on October 30 (121 days), unless they ordered again before.
    /// </summary>
    public static DateOnly? LostOn(IReadOnlyList<DateOnly> services, DateRange range, int inactiveDays, DateOnly today)
    {
        DateOnly? lost = null;
        for (var i = 0; i < services.Count; i++)
        {
            var day = services[i].AddDays(inactiveDays + 1);
            var next = i + 1 < services.Count ? services[i + 1] : (DateOnly?)null;
            if (range.Contains(day) && day <= today && (next is null || next > day))
            {
                lost = day;
            }
        }
        return lost;
    }

    /// <summary>Active at the close: a service in the <paramref name="inactiveDays"/> days up to <paramref name="close"/>.</summary>
    public static bool ActiveAt(IReadOnlyList<DateOnly> services, DateOnly close, int inactiveDays) =>
        services.Any(d => d <= close && close.DayNumber - d.DayNumber <= inactiveDays);

    /// <summary>
    /// Retention of a new invitado: whether they ordered again within <paramref name="days"/> of their first service.
    /// Null while those days have not passed yet (they do not count in the base). Example: first service on September 3
    /// and the second on September 28 → repeats at 30, 60 and 90 days.
    /// </summary>
    public static bool? RepeatedWithin(IReadOnlyList<DateOnly> services, int days, DateOnly today)
    {
        if (services.Count == 0)
        {
            return null;
        }
        var first = services[0];
        if (services.Count > 1 && services[1].DayNumber - first.DayNumber <= days)
        {
            return true;
        }
        return first.AddDays(days) <= today ? false : null;
    }

    /// <summary>"Noviembre 2026".</summary>
    public static string MonthLabel(DateOnly month)
    {
        var text = month.ToString("MMMM yyyy", Spanish);
        return char.ToUpper(text[0], Spanish) + text[1..];
    }
}
