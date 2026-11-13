using System.Globalization;
using CleanCom.Domain.Common;

namespace CleanCom.Domain.Agenda;

/// <summary>
/// The single agenda engine (docs/03-reglas-de-negocio.md, Agenda › disponibilidad, steps 3 and 4).
/// Pure: the caller resolves the zona, the minimum date and the occupancy. The office (Agendar), the invitado
/// app and the background jobs all go through here.
/// </summary>
public static class AgendaCalculator
{
    private static readonly CultureInfo Spanish = new("es-MX");

    public static AvailabilityResult FindAvailability(AvailabilityQuery q)
    {
        if (q.VisitDays.Count == 0)
        {
            throw new DomainException("La zona no tiene días de visita configurados.");
        }
        if (q.CupoParadas <= 0)
        {
            throw new DomainException("La ruta no tiene cupo de paradas configurado.");
        }

        var explanation = new List<string>
        {
            $"Días de visita de la zona: {DayList(q.VisitDays)}.",
            $"Fecha mínima por el corte: {Format(q.MinimumDate)}.",
        };

        DateOnly? chosen = null;
        var days = new List<AgendaDay>();
        // `days` holds the chosen day plus the next ones; stop once the other days are complete.
        for (var i = 0; i < q.SearchDays && days.Count <= q.OtherDaysCount; i++)
        {
            var date = q.MinimumDate.AddDays(i);
            if (!q.VisitDays.Contains(date.DayOfWeek))
            {
                continue;
            }
            if (q.NonWorkingDays.Contains(date))
            {
                if (chosen is null)
                {
                    explanation.Add($"{Format(date)}: día inhábil.");
                }
                continue;
            }

            var occupied = q.Occupancy.GetValueOrDefault(date);
            var available = occupied < q.CupoParadas;
            if (chosen is null)
            {
                if (!available)
                {
                    explanation.Add($"{Format(date)}: sin cupo ({occupied} de {q.CupoParadas}).");
                    continue;
                }
                chosen = date;
                explanation.Add($"{Format(date)}: primer día con cupo, lugar {occupied + 1} de {q.CupoParadas}.");
            }
            days.Add(new AgendaDay(date, occupied, q.CupoParadas, available));
        }

        if (chosen is null)
        {
            explanation.Add($"No hay días con cupo en los siguientes {q.SearchDays} días.");
            return new AvailabilityResult(null, null, q.CupoParadas, [], explanation);
        }

        var place = q.Occupancy.GetValueOrDefault(chosen.Value) + 1;
        return new AvailabilityResult(chosen, place, q.CupoParadas, days, explanation);
    }

    /// <summary>Short Spanish date, as the office reads it: "mar 3 nov".</summary>
    public static string Format(DateOnly date) =>
        date.ToString("ddd d MMM", Spanish).Replace(".", "", StringComparison.Ordinal);

    private static string DayList(IEnumerable<DayOfWeek> days)
    {
        // Monday first, as the office reads the week.
        var names = days.Distinct()
            .OrderBy(d => ((int)d + 6) % 7)
            .Select(d => Spanish.DateTimeFormat.GetDayName(d))
            .ToList();
        return names.Count == 1 ? names[0] : $"{string.Join(", ", names[..^1])} y {names[^1]}";
    }
}
