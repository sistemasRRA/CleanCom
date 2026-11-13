using CleanCom.Domain.Crm;

namespace CleanCom.Tests.Crm;

/// <summary>
/// docs/03-reglas-de-negocio.md, CRM › Frecuencia y estatus, and Registro de contacto.
/// Thresholds as the initial settings: nuevo 45 days, inactivo 120 days, en riesgo 2 times the frequency.
/// </summary>
public class CrmRulesTests
{
    private static readonly CrmThresholds T = new(45, 120, 2);
    private static readonly DateOnly Today = new(2026, 11, 13);

    private static DateOnly DaysAgo(int n) => Today.AddDays(-n);

    [Fact]
    public void Without_services_it_is_a_prospecto()
    {
        var e = CrmRules.Evaluate([], Today, null, T);
        Assert.Equal(CrmStatus.Prospecto, e.Status);
        Assert.Equal(0, e.ServiceCount);
    }

    [Fact]
    public void Frequency_is_the_median_of_days_between_services()
    {
        // Gaps of 7, 7 and 30 days: the median is 7, an unusual gap does not move it.
        Assert.Equal(7, CrmRules.MedianGap([new(2026, 9, 1), new(2026, 9, 8), new(2026, 9, 15), new(2026, 10, 15)]));
        // Even count: average of the two middle gaps (6 and 8).
        Assert.Equal(7, CrmRules.MedianGap([new(2026, 9, 1), new(2026, 9, 7), new(2026, 9, 15)]));
        Assert.Null(CrmRules.MedianGap([new(2026, 9, 1)]));
    }

    [Fact]
    public void Asks_every_7_days_and_last_service_9_days_ago_le_toca()
    {
        // Example of the document: "pide cada 7 días; su último servicio fue hace 9".
        var e = CrmRules.Evaluate([DaysAgo(23), DaysAgo(16), DaysAgo(9)], Today, Today.AddDays(1), T);
        Assert.Equal(CrmStatus.LeToca, e.Status);
        Assert.Equal(7, e.FrequencyDays);
        Assert.Equal(DaysAgo(2), e.LeTocaDate);
        Assert.StartsWith("Pide cada 7 días; su último servicio fue hace 9 días.", e.Why);
    }

    [Fact]
    public void Le_toca_when_the_date_arrives_before_the_next_zona_visit()
    {
        // Every 7 days, last one 5 days ago: le toca in 2 days, and the ruta comes in 3 days.
        var dates = new[] { DaysAgo(19), DaysAgo(12), DaysAgo(5) };
        Assert.Equal(CrmStatus.LeToca, CrmRules.Evaluate(dates, Today, Today.AddDays(3), T).Status);
        // If the ruta comes tomorrow, it is not yet due on that visit.
        Assert.NotEqual(CrmStatus.LeToca, CrmRules.Evaluate(dates, Today, Today.AddDays(1), T).Status);
    }

    [Fact]
    public void Twice_the_frequency_without_asking_is_en_riesgo()
    {
        var e = CrmRules.Evaluate([DaysAgo(28), DaysAgo(21), DaysAgo(14)], Today, null, T);
        Assert.Equal(CrmStatus.EnRiesgo, e.Status);
    }

    [Fact]
    public void More_than_120_days_without_a_service_is_inactivo()
    {
        Assert.Equal(CrmStatus.Inactivo, CrmRules.Evaluate([DaysAgo(200), DaysAgo(121)], Today, null, T).Status);
        Assert.NotEqual(CrmStatus.Inactivo, CrmRules.Evaluate([DaysAgo(200), DaysAgo(120)], Today, null, T).Status);
    }

    [Fact]
    public void First_service_in_the_last_45_days_is_nuevo()
    {
        Assert.Equal(CrmStatus.Nuevo, CrmRules.Evaluate([DaysAgo(10)], Today, null, T).Status);
        Assert.Equal(CrmStatus.Activo, CrmRules.Evaluate([DaysAgo(60)], Today, null, T).Status);
    }

    [Fact]
    public void Asking_with_the_usual_frequency_is_activo()
    {
        var e = CrmRules.Evaluate([DaysAgo(90), DaysAgo(60), DaysAgo(30), DaysAgo(2)], Today, Today.AddDays(1), T);
        Assert.Equal(CrmStatus.Activo, e.Status);
    }

    [Fact]
    public void A_fixed_frequency_wins_over_the_calculated_one()
    {
        // Calculated every 7 days (en riesgo after 14 days without asking); fixed at 30 by the office it is active.
        var e = CrmRules.Evaluate([DaysAgo(70), DaysAgo(63), DaysAgo(56), DaysAgo(14)], Today, null, T, fixedFrequencyDays: 30);
        Assert.Equal(30, e.FrequencyDays);
        Assert.Equal(CrmStatus.Activo, e.Status);
    }

    [Fact]
    public void Future_recolecciones_are_not_services_yet()
    {
        Assert.Equal(CrmStatus.Prospecto, CrmRules.Evaluate([Today.AddDays(3)], Today, null, T).Status);
    }

    [Fact]
    public void Next_visit_day_of_the_zona()
    {
        // Friday 13 → zona 1 (Monday and Thursday) is next visited on Monday 16.
        Assert.Equal(new DateOnly(2026, 11, 16), CrmRules.NextVisitDay(Today, [DayOfWeek.Monday, DayOfWeek.Thursday]));
        Assert.Equal(Today, CrmRules.NextVisitDay(Today, [DayOfWeek.Friday]));
        Assert.Null(CrmRules.NextVisitDay(Today, []));
    }

    [Theory]
    [InlineData(ContactResult.NoContesto, 1)]
    [InlineData(ContactResult.LlamarDespues, 3)]
    [InlineData(ContactResult.NoPorAhora, 21)]
    public void Without_a_date_the_next_contact_follows_the_result(ContactResult result, int days)
    {
        // "No contestó" → tomorrow; "pidió que le llamaran" → in 3 days; "no por ahora" → in 3 weeks.
        Assert.Equal(Today.AddDays(days), Contacto.DefaultNextContact(result, Today, new FollowUpDays(1, 3, 21)));
    }

    [Theory]
    [InlineData(ContactResult.Agendo)]
    [InlineData(ContactResult.NoContactar)]
    public void After_scheduling_or_do_not_contact_there_is_no_next_contact(ContactResult result)
    {
        Assert.Null(Contacto.DefaultNextContact(result, Today, new FollowUpDays(1, 3, 21)));
    }
}
