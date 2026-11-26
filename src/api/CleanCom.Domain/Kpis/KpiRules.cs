using System.Globalization;

namespace CleanCom.Domain.Kpis;

/// <summary>The period shown on the KPIs screen.</summary>
public enum KpiPeriod
{
    /// <summary>Monday to Sunday.</summary>
    Semana,
    Mes,
    Trimestre,
}

/// <summary>The period a goal is written for ("10 al día", "300 al mes").</summary>
public enum KpiGoalPeriod
{
    Dia,
    Semana,
    Mes,
}

public enum KpiLight
{
    SinMeta,
    Rojo,
    Verde,
    SuperVerde,
}

/// <summary>A business-date range, both ends included.</summary>
public readonly record struct DateRange(DateOnly Start, DateOnly End)
{
    public int Days => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateOnly date) => date >= Start && date <= End;
}

/// <summary>
/// One piece of a value: a visit, a ticket, a pedido… with what it adds to the numerator and the denominator.
/// <paramref name="Key"/> identifies the invitado for distinct counts.
/// </summary>
public readonly record struct KpiFact(int? RutaId, decimal Numerator, decimal Denominator, int? Key = null);

/// <summary>The thresholds of a goal as they apply to a period (already scaled). Only verde is required.</summary>
public readonly record struct KpiThresholds(decimal? Red, decimal Green, decimal? SuperGreen);

/// <summary>docs/03-reglas-de-negocio.md, KPIs: values, periods, the traffic light and goals with vigencia.</summary>
public static class KpiRules
{
    private static readonly CultureInfo Spanish = new("es-MX");

    /// <summary>The value of a set of facts; null when there is nothing to measure (no denominator, no facts).</summary>
    public static decimal? Value(KpiDefinition kpi, IEnumerable<KpiFact> facts)
    {
        var list = facts.ToList();
        decimal? raw = kpi.Aggregation switch
        {
            KpiAggregation.Ratio => list.Sum(f => f.Denominator) is var d && d == 0 ? null : list.Sum(f => f.Numerator) / d,
            KpiAggregation.Sum => list.Sum(f => f.Numerator),
            KpiAggregation.DistinctCount => list.Where(f => f.Numerator != 0 && f.Key is not null).Select(f => f.Key).Distinct().Count(),
            KpiAggregation.PerDistinct => list.Where(f => f.Key is not null).Select(f => f.Key).Distinct().Count() is var k && k == 0
                ? null
                : list.Sum(f => f.Numerator) / k,
            _ => null,
        };
        return raw is { } v ? Math.Round(v * kpi.Multiplier, 2, MidpointRounding.AwayFromZero) : null;
    }

    /// <summary>The period of a kind that contains <paramref name="date"/>. Weeks go from Monday to Sunday.</summary>
    public static DateRange Range(KpiPeriod period, DateOnly date) => period switch
    {
        KpiPeriod.Semana => new DateRange(date.AddDays(-(((int)date.DayOfWeek + 6) % 7)), date.AddDays(-(((int)date.DayOfWeek + 6) % 7) + 6)),
        KpiPeriod.Mes => new DateRange(new DateOnly(date.Year, date.Month, 1), new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month))),
        _ => QuarterOf(date),
    };

    /// <summary>The period right before the one that contains <paramref name="date"/>.</summary>
    public static DateRange Previous(KpiPeriod period, DateOnly date) => Range(period, Range(period, date).Start.AddDays(-1));

    /// <summary>"Semana del 23 al 29 nov 2026", "Noviembre 2026", "4.º trimestre 2026".</summary>
    public static string Label(KpiPeriod period, DateRange range) => period switch
    {
        KpiPeriod.Semana when range.Start.Month == range.End.Month =>
            $"Semana del {range.Start.Day} al {range.End.Day} {Month(range.End)} {range.End.Year}",
        KpiPeriod.Semana when range.Start.Year == range.End.Year =>
            $"Semana del {range.Start.Day} {Month(range.Start)} al {range.End.Day} {Month(range.End)} {range.End.Year}",
        KpiPeriod.Semana =>
            $"Semana del {range.Start.Day} {Month(range.Start)} {range.Start.Year} al {range.End.Day} {Month(range.End)} {range.End.Year}",
        KpiPeriod.Mes => Capitalize(range.Start.ToString("MMMM yyyy", Spanish)),
        _ => $"{(range.Start.Month - 1) / 3 + 1}.º trimestre {range.Start.Year}",
    };

    /// <summary>
    /// The part of a period that has happened: up to today while the period is open. A goal of a count is compared
    /// with these days, so on Wednesday a weekly goal is not red just because the week is not over.
    /// </summary>
    public static DateRange Elapsed(DateRange range, DateOnly today) =>
        range.Contains(today) && today < range.End ? new DateRange(range.Start, today) : range;

    /// <summary>
    /// The thresholds of a goal for the period shown, or null when the goal does not apply to it.
    /// <list type="bullet">
    /// <item>Averages and shares: as written.</item>
    /// <item>Counts and amounts that add up: scaled by <paramref name="elapsed"/> days. Per day by the days, per week by
    /// days / 7, per month by days / 30 (a whole calendar month or quarter counts as 1 or 3 months). Example: 20 clientes
    /// nuevos a la semana, seen for a whole month of 31 days → 20 × 31 / 7 = 88.57.</item>
    /// <item>Different invitados: only for a period of the goal's kind (weekly goal, weeks; monthly goal, months), in
    /// proportion to the days gone by while it is open. Example: 35 clientes atendidos a la semana; on Wednesday, 15.</item>
    /// </list>
    /// </summary>
    public static KpiThresholds? Scale(KpiDefinition kpi, KpiGoal goal, KpiPeriod period, DateRange range, DateRange elapsed)
    {
        decimal? factor = kpi.Scaling switch
        {
            KpiGoalScaling.None => 1m,
            KpiGoalScaling.SamePeriod => (goal.Period, period) is (KpiGoalPeriod.Semana, KpiPeriod.Semana) or (KpiGoalPeriod.Mes, KpiPeriod.Mes)
                ? (decimal)elapsed.Days / range.Days
                : null,
            _ => goal.Period switch
            {
                KpiGoalPeriod.Dia => elapsed.Days,
                KpiGoalPeriod.Semana => elapsed.Days / 7m,
                _ when elapsed == range && period == KpiPeriod.Mes => 1,
                _ when elapsed == range && period == KpiPeriod.Trimestre => 3,
                _ => elapsed.Days / 30m,
            },
        };
        if (factor is not { } f)
        {
            return null;
        }
        decimal Round(decimal v) => Math.Round(v * f, 2, MidpointRounding.AwayFromZero);
        return new KpiThresholds(goal.Red is { } red ? Round(red) : null, Round(goal.Green), goal.SuperGreen is { } super ? Round(super) : null);
    }

    /// <summary>
    /// What the verde of a count or an amount equals in each kind of period, with the same scaling as the screen (a
    /// month of 30 days, a quarter of 3 months or 13 weeks). Example: 6 a la semana → 0.86 al día, 25.71 al mes y 78 al
    /// trimestre. Empty for averages and shares (they do not change) and for different invitados (they only compare with
    /// their own kind of period).
    /// </summary>
    public static IReadOnlyList<(string Label, decimal Green)> Equivalences(KpiDefinition kpi, KpiGoal goal)
    {
        if (kpi.Scaling != KpiGoalScaling.ByDays)
        {
            return [];
        }
        var g = goal.Green;
        var (day, week, month, quarter) = goal.Period switch
        {
            KpiGoalPeriod.Dia => (g, g * 7, g * 30, g * 91),
            KpiGoalPeriod.Semana => (g / 7, g, g * 30 / 7, g * 13),
            _ => (g / 30, g * 7 / 30, g, g * 3),
        };
        decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        return [("al día", Round(day)), ("a la semana", Round(week)), ("al mes", Round(month)), ("al trimestre", Round(quarter))];
    }

    /// <summary>
    /// The goal of the whole company from the goals of each ruta, when there is no goal for the company and every active
    /// ruta has one: counts and amounts add up (clientes nuevos 6 en F y 6 en K → 12), averages and shares are averaged.
    /// A threshold that some ruta lacks is left out. Null when some ruta has no goal.
    /// </summary>
    public static KpiThresholds? Combine(KpiDefinition kpi, IReadOnlyList<KpiThresholds?> byRuta)
    {
        if (byRuta.Count == 0 || byRuta.Any(t => t is null))
        {
            return null;
        }
        var all = byRuta.Select(t => t!.Value).ToList();
        var add = kpi.Scaling != KpiGoalScaling.None;
        decimal Join(IEnumerable<decimal> values) =>
            Math.Round(add ? values.Sum() : values.Average(), 2, MidpointRounding.AwayFromZero);
        decimal? JoinOptional(Func<KpiThresholds, decimal?> pick) =>
            all.All(t => pick(t) is not null) ? Join(all.Select(t => pick(t)!.Value)) : null;
        return new KpiThresholds(JoinOptional(t => t.Red), Join(all.Select(t => t.Green)), JoinOptional(t => t.SuperGreen));
    }

    /// <summary>
    /// Súper verde when the value reaches súper verde, verde when it reaches verde, rojo otherwise ("reach" is ≥ when
    /// more is better and ≤ when less is better). Without súper verde it is never súper verde. No value or no goal: sin
    /// meta.
    /// </summary>
    public static KpiLight Light(decimal? value, KpiThresholds? goal, bool higherIsBetter)
    {
        if (value is not { } v || goal is not { } g)
        {
            return KpiLight.SinMeta;
        }
        bool Reaches(decimal threshold) => higherIsBetter ? v >= threshold : v <= threshold;
        return g.SuperGreen is { } super && Reaches(super) ? KpiLight.SuperVerde : Reaches(g.Green) ? KpiLight.Verde : KpiLight.Rojo;
    }

    /// <summary>
    /// The goal in force for a period, of the whole company (<paramref name="rutaId"/> null) or of one ruta: the latest
    /// active one that started by the period's last day (or today, while it is open). A later change does not repaint
    /// past periods.
    /// </summary>
    public static KpiGoal? GoalFor(IEnumerable<KpiGoal> goals, string key, int? rutaId, DateRange range, DateOnly today)
    {
        var at = range.End < today ? range.End : today;
        return goals.Where(g => g.Active && g.IndicadorKey == key && g.RutaId == rutaId && g.ValidFrom <= at)
            .OrderByDescending(g => g.ValidFrom).ThenByDescending(g => g.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// The order of the thresholds: rojo, verde, súper verde going up when more is better, going down when less is better.
    /// Rojo and súper verde are optional; the ones given must keep the order. Null when it is right; otherwise the message.
    /// </summary>
    public static string? OrderError(bool higherIsBetter, decimal? red, decimal green, decimal? superGreen)
    {
        bool Before(decimal a, decimal b) => higherIsBetter ? a < b : a > b;
        var right = (red is not { } r || Before(r, green)) && (superGreen is not { } s || Before(green, s));
        return right ? null
            : higherIsBetter ? "Como más es mejor, debe ir rojo < verde < súper verde."
            : "Como menos es mejor, debe ir rojo > verde > súper verde.";
    }

    private static DateRange QuarterOf(DateOnly date)
    {
        var firstMonth = (date.Month - 1) / 3 * 3 + 1;
        var start = new DateOnly(date.Year, firstMonth, 1);
        return new DateRange(start, start.AddMonths(3).AddDays(-1));
    }

    private static string Month(DateOnly date) => date.ToString("MMM", Spanish).TrimEnd('.');

    private static string Capitalize(string s) => char.ToUpperInvariant(s[0]) + s[1..];
}
