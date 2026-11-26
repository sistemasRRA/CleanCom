using CleanCom.Domain.Kpis;

namespace CleanCom.Tests.Kpis;

/// <summary>docs/03-reglas-de-negocio.md, KPIs › Avance contra la meta.</summary>
public class KpiProgressTests
{
    [Fact]
    public void Shows_how_much_is_missing_for_verde()
    {
        // Rojo 8, verde 10, súper verde 11 and 9: span 8 to 11 with a margin of 0.3 → 7.7 to 11.3.
        var p = KpiProgressRules.Of(9, new KpiThresholds(8, 10, 11), higherIsBetter: true)!;

        Assert.Equal(KpiLight.Verde, p.Next);
        Assert.Equal(1, p.Gap);
        Assert.Equal(36.1m, p.ValuePosition);
        Assert.Equal(63.9m, p.GreenPosition);
        Assert.True(p.RedPosition < p.ValuePosition && p.ValuePosition < p.GreenPosition && p.GreenPosition < p.SuperGreenPosition);
    }

    [Fact]
    public void After_verde_the_next_is_super_verde_and_after_that_nothing()
    {
        var t = new KpiThresholds(8, 10, 11);
        var green = KpiProgressRules.Of(10.4m, t, true)!;
        var super = KpiProgressRules.Of(12, t, true)!;

        Assert.Equal(KpiLight.SuperVerde, green.Next);
        Assert.Equal(0.6m, green.Gap);
        Assert.Null(super.Next);
        Assert.Null(super.Gap);
    }

    [Fact]
    public void When_less_is_better_the_good_side_is_still_on_the_right()
    {
        // Pedidos fuera de tiempo: rojo 8, verde 4, súper verde 2; 6 de ellos → faltan 2 para verde (bajar a 4).
        var p = KpiProgressRules.Of(6, new KpiThresholds(8, 4, 2), higherIsBetter: false)!;

        Assert.Equal(KpiLight.Verde, p.Next);
        Assert.Equal(2, p.Gap);
        Assert.True(p.RedPosition < p.ValuePosition && p.ValuePosition < p.GreenPosition && p.GreenPosition < p.SuperGreenPosition);
    }

    [Fact]
    public void Without_super_verde_reaching_verde_is_the_end()
    {
        // Tickets firmados: solo verde 100.
        var p = KpiProgressRules.Of(100, new KpiThresholds(null, 100, null), true)!;
        Assert.Null(p.Next);
        Assert.Null(p.RedPosition);
        Assert.Null(p.SuperGreenPosition);
    }

    [Fact]
    public void Without_value_or_goal_there_is_no_bar()
    {
        Assert.Null(KpiProgressRules.Of(null, new KpiThresholds(1, 2, 3), true));
        Assert.Null(KpiProgressRules.Of(5, null, true));
    }
}

public class KpiEquivalenceTests
{
    [Fact]
    public void A_weekly_count_equals_per_day_month_and_quarter()
    {
        // Clientes nuevos 6 a la semana → 0.86 al día, 25.71 al mes (30 días) y 78 al trimestre (13 semanas).
        var kpi = KpiCatalog.Find(KpiCatalog.ClientesNuevos)!;
        var e = KpiRules.Equivalences(kpi, new KpiGoal { IndicadorKey = kpi.Key, Period = KpiGoalPeriod.Semana, Green = 6 });
        Assert.Equal([("al día", 0.86m), ("a la semana", 6m), ("al mes", 25.71m), ("al trimestre", 78m)], e);
    }

    [Fact]
    public void A_monthly_amount_is_three_times_in_a_quarter()
    {
        var kpi = KpiCatalog.Find(KpiCatalog.FacturacionSinConvenios)!;
        var e = KpiRules.Equivalences(kpi, new KpiGoal { IndicadorKey = kpi.Key, Period = KpiGoalPeriod.Mes, Green = 120_000 });
        Assert.Equal(360_000m, e.Single(x => x.Label == "al trimestre").Green);
        Assert.Equal(28_000m, e.Single(x => x.Label == "a la semana").Green);
    }

    [Fact]
    public void Averages_and_different_invitados_have_no_equivalences()
    {
        Assert.Empty(KpiRules.Equivalences(KpiCatalog.Find(KpiCatalog.TicketPromedio)!, new KpiGoal { Green = 800 }));
        Assert.Empty(KpiRules.Equivalences(KpiCatalog.Find(KpiCatalog.ClientesAtendidos)!, new KpiGoal { Green = 87, Period = KpiGoalPeriod.Mes }));
    }
}
