namespace CleanCom.Domain.Kpis;

/// <summary>Which goal applies to an indicator in a period: to the whole company and to each ruta.</summary>
/// <param name="Goal">The company's goal; null when the total comes from the rutas or there is none.</param>
/// <param name="Total">Thresholds for the company's value (scaled to the period).</param>
/// <param name="FromRutas">The total is the sum (or average) of the goals of the rutas.</param>
/// <param name="ByRuta">Thresholds for each ruta's value; null when it has none.</param>
/// <param name="Note">Why there is a goal but no light in this period (a weekly goal of different invitados seen by month).</param>
public record KpiGoalResolution(
    KpiGoal? Goal,
    KpiThresholds? Total,
    bool FromRutas,
    IReadOnlyDictionary<int, KpiThresholds?> ByRuta,
    string? Note);

public static class KpiGoalResolver
{
    /// <summary>
    /// docs/03-reglas-de-negocio.md, KPIs › Metas por ruta. A ruta uses its own goal; without one, it uses the company's
    /// only for averages and shares (a count of the company is not a count of one ruta). The company uses its own goal;
    /// without one, the goals of every active ruta combined (<see cref="KpiRules.Combine"/>).
    /// </summary>
    public static KpiGoalResolution Resolve(
        KpiDefinition kpi,
        IReadOnlyCollection<KpiGoal> goals,
        IReadOnlyList<int> rutaIds,
        KpiPeriod period,
        DateRange range,
        DateOnly today)
    {
        var elapsed = KpiRules.Elapsed(range, today);
        KpiThresholds? Scale(KpiGoal? g) => g is null ? null : KpiRules.Scale(kpi, g, period, range, elapsed);

        var company = KpiRules.GoalFor(goals, kpi.Key, null, range, today);
        var companyThresholds = Scale(company);
        var rutaGoals = rutaIds.ToDictionary(id => id, id => KpiRules.GoalFor(goals, kpi.Key, id, range, today));
        var byRuta = rutaIds.ToDictionary(id => id, id => rutaGoals[id] is { } own
            ? Scale(own)
            : kpi.Scaling == KpiGoalScaling.None ? companyThresholds : null);

        var total = company is not null ? companyThresholds : KpiRules.Combine(kpi, rutaIds.Select(id => Scale(rutaGoals[id])).ToList());
        var governing = company ?? rutaGoals.Values.FirstOrDefault(g => g is not null);
        string? note = governing is not null && Scale(governing) is null
            ? $"La meta es {(governing.Period == KpiGoalPeriod.Mes ? "mensual" : "semanal")}: se compara en la vista por {(governing.Period == KpiGoalPeriod.Mes ? "mes" : "semana")}."
            : null;
        return new KpiGoalResolution(company, total, company is null && total is not null, byRuta, note);
    }
}
