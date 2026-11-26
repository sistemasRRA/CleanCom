using CleanCom.Domain.Crm;
using CleanCom.Domain.Kpis;

namespace CleanCom.Tests.Crm;

/// <summary>Clientes y fuentes (docs/03-reglas-de-negocio.md, CRM › Clientes y fuentes), with 120 days.</summary>
public class ClientesRulesTests
{
    private const int Days = 120;
    private static readonly DateRange November = new(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));
    private static readonly DateOnly Today = new(2026, 11, 26);

    private static List<DateOnly> D(params string[] dates) => dates.Select(DateOnly.Parse).ToList();

    [Fact]
    public void Each_invitado_attended_counts_as_new_recurrent_or_recovered()
    {
        Assert.Equal(AttendedKind.Nuevo, ClientesRules.Attended(D("2026-11-10", "2026-11-20"), November, Days));
        // Previous service 41 days before.
        Assert.Equal(AttendedKind.Recurrente, ClientesRules.Attended(D("2026-10-01", "2026-11-10"), November, Days));
        // Exactly 120 days is still recurrent; 121 is recovered.
        Assert.Equal(AttendedKind.Recurrente, ClientesRules.Attended(D("2026-07-13", "2026-11-10"), November, Days));
        Assert.Equal(AttendedKind.Recuperado, ClientesRules.Attended(D("2026-07-12", "2026-11-10"), November, Days));
        Assert.Null(ClientesRules.Attended(D("2026-10-01"), November, Days));
    }

    [Fact]
    public void An_invitado_is_lost_the_day_they_complete_more_than_120_days_without_a_service()
    {
        // Example of the document: last service on July 1 → perdido on October 30.
        Assert.Equal(new DateOnly(2026, 10, 30), ClientesRules.LostOn(D("2026-07-01"), new DateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)), Days, Today));
        Assert.Null(ClientesRules.LostOn(D("2026-07-01"), November, Days, Today));
        // Ordered again before that day: not lost.
        Assert.Null(ClientesRules.LostOn(D("2026-07-15", "2026-11-05"), November, Days, Today));
        // Lost on November 12 and recovered on the 20th: perdido and recuperado in the same month.
        var services = D("2026-07-14", "2026-11-20");
        Assert.Equal(new DateOnly(2026, 11, 12), ClientesRules.LostOn(services, November, Days, Today));
        Assert.Equal(AttendedKind.Recuperado, ClientesRules.Attended(services, November, Days));
        // Not lost yet if the day has not come.
        Assert.Null(ClientesRules.LostOn(D("2026-08-01"), November, Days, Today));
    }

    [Fact]
    public void Active_at_the_close_ordered_in_the_120_days_before()
    {
        Assert.True(ClientesRules.ActiveAt(D("2026-07-29"), Today, Days));
        Assert.False(ClientesRules.ActiveAt(D("2026-07-28"), Today, Days));
        Assert.False(ClientesRules.ActiveAt(D("2026-11-27"), Today, Days));
    }

    [Fact]
    public void An_open_period_is_compared_with_the_same_days_of_the_previous_one()
    {
        var elapsed = new DateRange(November.Start, Today);
        Assert.Equal(new DateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 26)), ClientesRules.Comparison(ClientesPeriod.Mes, November, elapsed));
        var october = new DateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
        Assert.Equal(new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)), ClientesRules.Comparison(ClientesPeriod.Mes, october, october));
        // March 31 against February: up to its last day.
        var march = new DateRange(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 31));
        Assert.Equal(new DateOnly(2027, 2, 28), ClientesRules.Comparison(ClientesPeriod.Mes, march, new DateRange(march.Start, new DateOnly(2027, 3, 30))).End);
    }

    [Fact]
    public void The_year_goes_from_january_to_december()
    {
        var year = ClientesRules.Range(ClientesPeriod.Anio, Today);
        Assert.Equal(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), year);
        Assert.Equal("2026", ClientesRules.Label(ClientesPeriod.Anio, year));
        Assert.Equal("Noviembre 2026", ClientesRules.Label(ClientesPeriod.Mes, November));
    }

    [Fact]
    public void Retention_counts_only_when_the_days_have_passed()
    {
        // Example of the document: first service September 3, second September 28 → repeats at 30, 60 and 90.
        var services = D("2026-09-03", "2026-09-28");
        Assert.True(ClientesRules.RepeatedWithin(services, 30, Today));
        Assert.True(ClientesRules.RepeatedWithin(services, 90, Today));
        // Second service 45 days later: not at 30, yes at 60.
        var late = D("2026-08-01", "2026-09-15");
        Assert.False(ClientesRules.RepeatedWithin(late, 30, Today));
        Assert.True(ClientesRules.RepeatedWithin(late, 60, Today));
        // Only one service from November 10: the 30 days have not passed yet.
        Assert.Null(ClientesRules.RepeatedWithin(D("2026-11-10"), 30, Today));
        Assert.False(ClientesRules.RepeatedWithin(D("2026-10-01"), 30, Today));
    }
}
