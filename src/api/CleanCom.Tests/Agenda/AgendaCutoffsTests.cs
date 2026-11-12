using CleanCom.Domain.Agenda;
using CleanCom.Domain.Time;

namespace CleanCom.Tests.Agenda;

/// <summary>
/// docs/03-reglas-de-negocio.md, Agenda: cutoffs for new recolecciones, day changes and cancellations.
/// </summary>
public class AgendaCutoffsTests
{
    private static readonly TimeOnly Corte13 = new(13, 0);
    private static readonly TimeOnly Corte12 = new(12, 0);

    /// <summary>Instant from a Mexico City date and time in November 2026.</summary>
    private static DateTimeOffset Nov(int day, int hour, int minute) =>
        BusinessTime.ToUtc(new DateOnly(2026, 11, day), new TimeOnly(hour, minute));

    private static DateOnly Day(int day) => new(2026, 11, day);

    [Fact]
    public void Registered_before_the_same_day_cutoff_it_can_be_today()
    {
        Assert.Equal(Day(2), AgendaCutoffs.MinimumDate(Nov(2, 12, 59), Corte13));
    }

    [Fact]
    public void Registered_at_the_cutoff_it_no_longer_enters_the_same_day()
    {
        Assert.Equal(Day(3), AgendaCutoffs.MinimumDate(Nov(2, 13, 0), Corte13));
    }

    [Fact]
    public void Registered_at_18_51_after_the_13_00_cutoff_it_does_not_enter_the_same_day()
    {
        Assert.Equal(Day(3), AgendaCutoffs.MinimumDate(Nov(2, 18, 51), Corte13));
    }

    [Fact]
    public void The_cutoff_uses_Mexico_City_time_not_UTC()
    {
        // 20:30 in Mexico City is already the next day in UTC (02:30). The business date is still day 2.
        Assert.Equal(Day(3), AgendaCutoffs.MinimumDate(Nov(2, 20, 30), Corte13));
    }

    [Fact]
    public void Day_change_on_day_2_at_11_59_moves_to_day_3()
    {
        Assert.Equal(Day(3), AgendaCutoffs.MinimumDateForDayChange(Nov(2, 11, 59), Corte12));
    }

    [Fact]
    public void Day_change_on_day_2_at_12_01_moves_to_day_4()
    {
        Assert.Equal(Day(4), AgendaCutoffs.MinimumDateForDayChange(Nov(2, 12, 1), Corte12));
    }

    [Fact]
    public void Day_change_exactly_at_the_cutoff_already_moves_two_days()
    {
        Assert.Equal(Day(4), AgendaCutoffs.MinimumDateForDayChange(Nov(2, 12, 0), Corte12));
    }

    [Fact]
    public void Recoleccion_on_day_5_can_be_cancelled_until_day_4_at_13_00()
    {
        Assert.Equal(Nov(4, 13, 0), AgendaCutoffs.CancellationDeadline(Day(5), Corte13));
        Assert.True(AgendaCutoffs.CanCancelFromApp(Nov(4, 12, 59), Day(5), Corte13));
        Assert.True(AgendaCutoffs.CanCancelFromApp(Nov(4, 13, 0), Day(5), Corte13));
        Assert.False(AgendaCutoffs.CanCancelFromApp(Nov(4, 13, 1), Day(5), Corte13));
    }
}
