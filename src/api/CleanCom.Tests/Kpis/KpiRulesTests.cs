using CleanCom.Domain.Kpis;

namespace CleanCom.Tests.Kpis;

/// <summary>docs/03-reglas-de-negocio.md, KPIs: values, periods, traffic light and goals with vigencia.</summary>
public class KpiRulesTests
{
    private static KpiDefinition Kpi(string key) => KpiCatalog.Find(key)!;

    private static KpiGoal Goal(string key, decimal red, decimal green, decimal superGreen, DateOnly from, KpiGoalPeriod period = KpiGoalPeriod.Semana, int id = 1) =>
        new() { Id = id, IndicadorKey = key, Red = red, Green = green, SuperGreen = superGreen, ValidFrom = from, Period = period };

    [Fact]
    public void Every_indicator_of_the_rules_is_in_the_catalog_once()
    {
        Assert.Equal(16, KpiCatalog.All.Count);
        Assert.Equal(KpiCatalog.All.Count, KpiCatalog.All.Select(k => k.Key).Distinct().Count());
        Assert.Equal(3, KpiCatalog.All.Select(k => k.Area).Distinct().Count());
    }

    [Fact]
    public void Recolecciones_por_ruta_al_dia_divides_by_the_days_each_ruta_operated()
    {
        // Ruta F: 18 recolecciones in 2 days; ruta K: 6 in 1 day → 24 / 3 = 8.
        KpiFact[] facts =
        [
            .. Enumerable.Repeat(new KpiFact(1, 1, 0), 18), new(1, 0, 1), new(1, 0, 1),
            .. Enumerable.Repeat(new KpiFact(2, 1, 0), 6), new(2, 0, 1),
        ];
        var kpi = Kpi(KpiCatalog.RecoleccionesPorRutaDia);

        Assert.Equal(8m, KpiRules.Value(kpi, facts));
        Assert.Equal(9m, KpiRules.Value(kpi, facts.Where(f => f.RutaId == 1)));
        Assert.Equal(6m, KpiRules.Value(kpi, facts.Where(f => f.RutaId == 2)));
    }

    [Fact]
    public void A_share_is_a_percentage_and_without_denominator_there_is_no_value()
    {
        // 3 of 4 entregas with firma → 75 %.
        var kpi = Kpi(KpiCatalog.TicketsFirmados);
        Assert.Equal(75m, KpiRules.Value(kpi, [new(1, 1, 1), new(1, 1, 1), new(1, 1, 1), new(1, 0, 1)]));
        Assert.Null(KpiRules.Value(kpi, []));
    }

    [Fact]
    public void Nps_is_promoters_minus_detractors()
    {
        // 6 promotores, 2 pasivos y 2 detractores de 10 → 60 % − 20 % = 40.
        KpiFact[] facts = [.. Enumerable.Repeat(new KpiFact(null, 1, 1), 6), .. Enumerable.Repeat(new KpiFact(null, 0, 1), 2), .. Enumerable.Repeat(new KpiFact(null, -1, 1), 2)];
        Assert.Equal(40m, KpiRules.Value(Kpi(KpiCatalog.Nps), facts));
    }

    [Fact]
    public void Clientes_atendidos_counts_each_invitado_once()
    {
        // Invitado 7 visited by F and by K counts once in the total and once in each ruta.
        KpiFact[] facts = [new(1, 1, 0, 7), new(2, 1, 0, 7), new(1, 1, 0, 8)];
        var kpi = Kpi(KpiCatalog.ClientesAtendidos);
        Assert.Equal(2m, KpiRules.Value(kpi, facts));
        Assert.Equal(1m, KpiRules.Value(kpi, facts.Where(f => f.RutaId == 2)));
    }

    [Fact]
    public void Recurrencia_is_services_per_invitado()
    {
        // 5 services of 2 invitados → 2.5.
        KpiFact[] facts = [new(1, 1, 0, 7), new(1, 1, 0, 7), new(1, 1, 0, 7), new(1, 1, 0, 8), new(1, 1, 0, 8)];
        Assert.Equal(2.5m, KpiRules.Value(Kpi(KpiCatalog.Recurrencia), facts));
    }

    [Fact]
    public void A_week_goes_from_monday_to_sunday()
    {
        // Wednesday 25 nov 2026 → Monday 23 to Sunday 29.
        var week = KpiRules.Range(KpiPeriod.Semana, new DateOnly(2026, 11, 25));
        Assert.Equal(new DateRange(new(2026, 11, 23), new(2026, 11, 29)), week);
        Assert.Equal(week, KpiRules.Range(KpiPeriod.Semana, new DateOnly(2026, 11, 29)));
        Assert.Equal("Semana del 23 al 29 nov 2026", KpiRules.Label(KpiPeriod.Semana, week));
        Assert.Equal(new DateRange(new(2026, 11, 16), new(2026, 11, 22)), KpiRules.Previous(KpiPeriod.Semana, new DateOnly(2026, 11, 25)));
    }

    [Fact]
    public void Months_and_quarters_are_calendar_ones()
    {
        var month = KpiRules.Range(KpiPeriod.Mes, new DateOnly(2026, 2, 10));
        Assert.Equal(new DateRange(new(2026, 2, 1), new(2026, 2, 28)), month);
        Assert.Equal("Febrero 2026", KpiRules.Label(KpiPeriod.Mes, month));

        var quarter = KpiRules.Range(KpiPeriod.Trimestre, new DateOnly(2026, 11, 25));
        Assert.Equal(new DateRange(new(2026, 10, 1), new(2026, 12, 31)), quarter);
        Assert.Equal("4.º trimestre 2026", KpiRules.Label(KpiPeriod.Trimestre, quarter));
        Assert.Equal(new DateRange(new(2026, 7, 1), new(2026, 9, 30)), KpiRules.Previous(KpiPeriod.Trimestre, new DateOnly(2026, 11, 25)));
    }

    [Theory]
    [InlineData(11, KpiLight.SuperVerde)]
    [InlineData(10, KpiLight.Verde)]
    [InlineData(9.9, KpiLight.Rojo)]
    [InlineData(7, KpiLight.Rojo)]
    public void More_is_better_light(decimal value, KpiLight expected)
    {
        // Recolecciones por ruta al día: rojo 8, verde 10, súper verde 11.
        Assert.Equal(expected, KpiRules.Light(value, new KpiThresholds(8, 10, 11), higherIsBetter: true));
    }

    [Theory]
    [InlineData(0, KpiLight.SuperVerde)]
    [InlineData(2, KpiLight.Verde)]
    [InlineData(3, KpiLight.Rojo)]
    public void Less_is_better_light(decimal value, KpiLight expected)
    {
        // Pedidos fuera de tiempo: rojo 5, verde 2, súper verde 0.
        Assert.Equal(expected, KpiRules.Light(value, new KpiThresholds(5, 2, 0), higherIsBetter: false));
    }

    [Fact]
    public void Without_value_or_goal_there_is_no_light()
    {
        Assert.Equal(KpiLight.SinMeta, KpiRules.Light(null, new KpiThresholds(1, 2, 3), true));
        Assert.Equal(KpiLight.SinMeta, KpiRules.Light(5, null, true));
    }

    [Fact]
    public void A_goal_change_does_not_repaint_past_periods()
    {
        // Verde 8 since 1 nov; changed to verde 10 on Wednesday 25 nov.
        var key = KpiCatalog.RecoleccionesPorRutaDia;
        KpiGoal[] goals = [Goal(key, 6, 8, 9, new(2026, 11, 1), id: 1), Goal(key, 8, 10, 11, new(2026, 11, 25), id: 2)];
        var today = new DateOnly(2026, 11, 26);

        var lastWeek = KpiRules.Range(KpiPeriod.Semana, new DateOnly(2026, 11, 18));
        var thisWeek = KpiRules.Range(KpiPeriod.Semana, today);
        var october = KpiRules.Range(KpiPeriod.Mes, new DateOnly(2026, 10, 15));

        Assert.Equal(8, KpiRules.GoalFor(goals, key, null, lastWeek, today)!.Green);
        Assert.Equal(10, KpiRules.GoalFor(goals, key, null, thisWeek, today)!.Green);
        Assert.Null(KpiRules.GoalFor(goals, key, null, october, today));
    }

    [Fact]
    public void A_goal_that_starts_later_does_not_apply_yet()
    {
        var key = KpiCatalog.VisitasConFoto;
        KpiGoal[] goals = [Goal(key, 80, 90, 95, new(2026, 12, 1))];
        var today = new DateOnly(2026, 11, 25);
        Assert.Null(KpiRules.GoalFor(goals, key, null, KpiRules.Range(KpiPeriod.Mes, today), today));
    }

    [Fact]
    public void An_inactive_goal_is_ignored()
    {
        var key = KpiCatalog.VisitasConFoto;
        var goal = Goal(key, 80, 90, 95, new(2026, 11, 1));
        goal.Active = false;
        var today = new DateOnly(2026, 11, 25);
        Assert.Null(KpiRules.GoalFor([goal], key, null, KpiRules.Range(KpiPeriod.Semana, today), today));
    }

    [Fact]
    public void A_count_goal_is_scaled_to_the_days_shown()
    {
        // 20 clientes nuevos a la semana; a whole month of 31 days → 20 × 31 / 7 = 88.57.
        var kpi = Kpi(KpiCatalog.ClientesNuevos);
        var goal = Goal(kpi.Key, 10, 20, 30, new(2026, 1, 1));
        var december = KpiRules.Range(KpiPeriod.Mes, new DateOnly(2026, 12, 10));
        var scaled = KpiRules.Scale(kpi, goal, KpiPeriod.Mes, december, december);
        Assert.Equal(88.57m, scaled!.Value.Green);
    }

    [Fact]
    public void A_count_goal_of_an_open_period_uses_the_days_gone_by()
    {
        // 20 a la semana, Wednesday of the current week: 3 days → 8.57; on Wednesday it is not red for the whole week.
        var kpi = Kpi(KpiCatalog.ClientesNuevos);
        var goal = Goal(kpi.Key, 10, 20, 30, new(2026, 1, 1));
        var today = new DateOnly(2026, 11, 25);
        var week = KpiRules.Range(KpiPeriod.Semana, today);
        var elapsed = KpiRules.Elapsed(week, today);

        Assert.Equal(3, elapsed.Days);
        Assert.Equal(8.57m, KpiRules.Scale(kpi, goal, KpiPeriod.Semana, week, elapsed)!.Value.Green);
    }

    [Fact]
    public void A_monthly_goal_counts_whole_months_in_a_quarter()
    {
        // Facturación: verde $300,000 al mes → $900,000 in a whole quarter.
        var kpi = Kpi(KpiCatalog.FacturacionSinConvenios);
        var goal = Goal(kpi.Key, 250_000, 300_000, 350_000, new(2026, 1, 1), KpiGoalPeriod.Mes);
        var quarter = KpiRules.Range(KpiPeriod.Trimestre, new DateOnly(2026, 8, 1));
        Assert.Equal(900_000m, KpiRules.Scale(kpi, goal, KpiPeriod.Trimestre, quarter, quarter)!.Value.Green);
    }

    [Fact]
    public void An_average_goal_is_not_scaled()
    {
        // Recolecciones por ruta al día is already per day: verde 10 for a week, a month or a quarter.
        var kpi = Kpi(KpiCatalog.RecoleccionesPorRutaDia);
        var goal = Goal(kpi.Key, 8, 10, 11, new(2026, 1, 1), KpiGoalPeriod.Dia);
        var quarter = KpiRules.Range(KpiPeriod.Trimestre, new DateOnly(2026, 8, 1));
        Assert.Equal(new KpiThresholds(8, 10, 11), KpiRules.Scale(kpi, goal, KpiPeriod.Trimestre, quarter, quarter));
    }

    [Fact]
    public void A_goal_of_different_invitados_only_applies_to_its_own_kind_of_period()
    {
        // 35 clientes atendidos a la semana: the same invitados come back, so a month is not 4.4 weeks.
        var kpi = Kpi(KpiCatalog.ClientesAtendidos);
        var goal = Goal(kpi.Key, 30, 35, 45, new(2026, 1, 1));
        var week = KpiRules.Range(KpiPeriod.Semana, new DateOnly(2026, 11, 18));
        var month = KpiRules.Range(KpiPeriod.Mes, new DateOnly(2026, 10, 10));

        Assert.Equal(new KpiThresholds(30, 35, 45), KpiRules.Scale(kpi, goal, KpiPeriod.Semana, week, week));
        Assert.Null(KpiRules.Scale(kpi, goal, KpiPeriod.Mes, month, month));
    }

    [Fact]
    public void A_goal_of_different_invitados_in_an_open_week_is_proportional_to_the_days_gone_by()
    {
        // 35 a la semana; on Wednesday (3 of 7 days) → 15.
        var kpi = Kpi(KpiCatalog.ClientesAtendidos);
        var goal = Goal(kpi.Key, 30, 35, 45, new(2026, 1, 1));
        var today = new DateOnly(2026, 11, 25);
        var week = KpiRules.Range(KpiPeriod.Semana, today);
        Assert.Equal(15m, KpiRules.Scale(kpi, goal, KpiPeriod.Semana, week, KpiRules.Elapsed(week, today))!.Value.Green);
    }

    [Fact]
    public void Clientes_nuevos_add_up_because_each_invitado_is_new_once()
    {
        Assert.Equal(KpiGoalScaling.ByDays, Kpi(KpiCatalog.ClientesNuevos).Scaling);
        Assert.Equal(KpiGoalScaling.SamePeriod, Kpi(KpiCatalog.ClientesAtendidos).Scaling);
        Assert.Equal(KpiGoalScaling.None, Kpi(KpiCatalog.Recurrencia).Scaling);
    }

    [Fact]
    public void A_period_that_has_not_started_is_taken_whole()
    {
        var today = new DateOnly(2026, 11, 25);
        var next = KpiRules.Range(KpiPeriod.Semana, new DateOnly(2026, 12, 10));
        Assert.Equal(next, KpiRules.Elapsed(next, today));
    }

    [Fact]
    public void Thresholds_must_go_in_order()
    {
        Assert.Null(KpiRules.OrderError(higherIsBetter: true, 8, 10, 11));
        Assert.NotNull(KpiRules.OrderError(higherIsBetter: true, 10, 8, 11));
        Assert.Null(KpiRules.OrderError(higherIsBetter: false, 5, 2, 0));
        Assert.NotNull(KpiRules.OrderError(higherIsBetter: false, 0, 2, 5));
    }

    [Fact]
    public void Rojo_and_super_verde_are_optional()
    {
        // Tickets firmados: only verde 100 (sheet "KPI's 2026"). Never súper verde; below 100 is rojo.
        var onlyGreen = new KpiThresholds(null, 100, null);
        Assert.Equal(KpiLight.Verde, KpiRules.Light(100, onlyGreen, true));
        Assert.Equal(KpiLight.Rojo, KpiRules.Light(97, onlyGreen, true));
        Assert.Null(KpiRules.OrderError(higherIsBetter: true, null, 100, null));
        Assert.NotNull(KpiRules.OrderError(higherIsBetter: true, null, 10, 8));
    }

    [Fact]
    public void A_scaled_goal_keeps_the_missing_thresholds_empty()
    {
        var kpi = Kpi(KpiCatalog.ClientesNuevos);
        var goal = new KpiGoal { IndicadorKey = kpi.Key, Green = 6, Period = KpiGoalPeriod.Semana, ValidFrom = new(2026, 1, 1) };
        var month = KpiRules.Range(KpiPeriod.Mes, new DateOnly(2026, 2, 10));
        Assert.Equal(new KpiThresholds(null, 24, null), KpiRules.Scale(kpi, goal, KpiPeriod.Mes, month, month));
    }

    private static KpiGoal RutaGoal(string key, int? rutaId, decimal? red, decimal green, decimal? superGreen, KpiGoalPeriod period = KpiGoalPeriod.Semana) =>
        new() { IndicadorKey = key, RutaId = rutaId, Red = red, Green = green, SuperGreen = superGreen, Period = period, ValidFrom = new(2026, 1, 1) };

    private static readonly DateOnly Today = new(2026, 11, 25);
    private static readonly DateRange LastWeek = KpiRules.Range(KpiPeriod.Semana, new DateOnly(2026, 11, 18));

    [Fact]
    public void Each_ruta_uses_its_own_goal_and_the_company_its_own()
    {
        // Facturación: total 24,613 / 29,613 / 34,613; F 14,463 / 16,963 / 19,463; K 10,150 / 12,650 / 15,150 a la semana.
        var kpi = Kpi(KpiCatalog.FacturacionSinConvenios);
        KpiGoal[] goals =
        [
            RutaGoal(kpi.Key, null, 24_613, 29_613, 34_613), RutaGoal(kpi.Key, 3, 14_463, 16_963, 19_463), RutaGoal(kpi.Key, 4, 10_150, 12_650, 15_150),
        ];
        var r = KpiGoalResolver.Resolve(kpi, goals, [3, 4], KpiPeriod.Semana, LastWeek, Today);

        Assert.Equal(29_613, r.Total!.Value.Green);
        Assert.False(r.FromRutas);
        Assert.Equal(16_963, r.ByRuta[3]!.Value.Green);
        Assert.Equal(12_650, r.ByRuta[4]!.Value.Green);
    }

    [Fact]
    public void Without_a_company_goal_the_total_adds_the_goals_of_the_rutas()
    {
        // Clientes nuevos por ruta: 4 / 6 / 8 a la semana in F and in K → 8 / 12 / 16 for the company.
        var kpi = Kpi(KpiCatalog.ClientesNuevos);
        KpiGoal[] goals = [RutaGoal(kpi.Key, 3, 4, 6, 8), RutaGoal(kpi.Key, 4, 4, 6, 8)];
        var r = KpiGoalResolver.Resolve(kpi, goals, [3, 4], KpiPeriod.Semana, LastWeek, Today);

        Assert.Equal(new KpiThresholds(8, 12, 16), r.Total);
        Assert.True(r.FromRutas);
    }

    [Fact]
    public void Without_a_goal_in_every_ruta_the_company_has_none()
    {
        var kpi = Kpi(KpiCatalog.ClientesNuevos);
        var r = KpiGoalResolver.Resolve(kpi, [RutaGoal(kpi.Key, 3, 4, 6, 8)], [3, 4], KpiPeriod.Semana, LastWeek, Today);
        Assert.Null(r.Total);
        Assert.Null(r.ByRuta[4]);
    }

    [Fact]
    public void A_company_goal_of_an_average_applies_to_each_ruta_but_a_count_does_not()
    {
        // Recolecciones por ruta al día, verde 10: also each ruta's. Toques 50 a la semana: the company's only.
        var average = Kpi(KpiCatalog.RecoleccionesPorRutaDia);
        var count = Kpi(KpiCatalog.ToquesWhatsApp);
        Assert.Equal(10, KpiGoalResolver.Resolve(average, [RutaGoal(average.Key, null, null, 10, null, KpiGoalPeriod.Dia)], [3, 4], KpiPeriod.Semana, LastWeek, Today).ByRuta[3]!.Value.Green);
        Assert.Null(KpiGoalResolver.Resolve(count, [RutaGoal(count.Key, null, 40, 50, 70)], [3, 4], KpiPeriod.Semana, LastWeek, Today).ByRuta[3]);
    }

    [Fact]
    public void Monthly_goals_of_different_invitados_per_ruta_say_why_a_week_has_no_light()
    {
        // Clientes atendidos por ruta: 43 / 87 / 130 al mes.
        var kpi = Kpi(KpiCatalog.ClientesAtendidos);
        KpiGoal[] goals = [RutaGoal(kpi.Key, 3, 43, 87, 130, KpiGoalPeriod.Mes), RutaGoal(kpi.Key, 4, 43, 87, 130, KpiGoalPeriod.Mes)];
        var week = KpiGoalResolver.Resolve(kpi, goals, [3, 4], KpiPeriod.Semana, LastWeek, Today);
        var october = KpiGoalResolver.Resolve(kpi, goals, [3, 4], KpiPeriod.Mes, KpiRules.Range(KpiPeriod.Mes, new DateOnly(2026, 10, 1)), Today);

        Assert.Null(week.Total);
        Assert.Equal("La meta es mensual: se compara en la vista por mes.", week.Note);
        Assert.Equal(174, october.Total!.Value.Green);
        Assert.Equal(87, october.ByRuta[3]!.Value.Green);
    }
}
