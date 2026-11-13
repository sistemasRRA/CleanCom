using CleanCom.Domain.Agenda;
using CleanCom.Domain.Common;

namespace CleanCom.Tests.Agenda;

/// <summary>
/// docs/03-reglas-de-negocio.md, Agenda › disponibilidad and Paradas fijas (they take cupo).
/// November 2026: Monday 2, Tuesday 3, Wednesday 4, Thursday 5, Friday 6, Saturday 7.
/// </summary>
public class AgendaCalculatorTests
{
    private static readonly DayOfWeek[] MondayToSaturday =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday];

    private static readonly DayOfWeek[] Zona3 = [DayOfWeek.Wednesday, DayOfWeek.Saturday];

    private static DateOnly Day(int day) => new(2026, 11, day);

    private static AvailabilityQuery Query(
        DateOnly minimum,
        DayOfWeek[] visitDays,
        Dictionary<DateOnly, int>? occupancy = null,
        DateOnly[]? nonWorking = null,
        int cupo = 14,
        int otherDays = 6) =>
        new(minimum, visitDays, (nonWorking ?? []).ToHashSet(), occupancy ?? [], cupo, otherDays);

    [Fact]
    public void Zona_K3_registered_after_the_cutoff_gets_tuesday_3_place_9_of_14()
    {
        // Example from the current system (all zonas marked Monday to Saturday): registered Monday 2 at 18:51,
        // so the minimum date is Tuesday 3, which has 8 paradas.
        var result = AgendaCalculator.FindAvailability(Query(Day(3), MondayToSaturday, new() { [Day(3)] = 8 }));

        Assert.Equal(Day(3), result.Date);
        Assert.Equal(9, result.Place);
        Assert.Equal(14, result.Cupo);
        Assert.Contains(result.Explanation, line => line.Contains("mar 3 nov") && line.Contains("lugar 9 de 14"));
    }

    [Fact]
    public void Only_visit_days_of_the_zona_are_offered()
    {
        // Zona 3 is visited Wednesday and Saturday: from Monday 2 the first date is Wednesday 4.
        var result = AgendaCalculator.FindAvailability(Query(Day(2), Zona3));
        Assert.Equal(Day(4), result.Date);
        Assert.Equal(1, result.Place);
    }

    [Fact]
    public void A_full_day_is_skipped_for_the_next_visit_day_with_cupo()
    {
        var result = AgendaCalculator.FindAvailability(Query(Day(2), Zona3, new() { [Day(4)] = 14, [Day(7)] = 5 }));

        Assert.Equal(Day(7), result.Date);
        Assert.Equal(6, result.Place);
        Assert.Contains(result.Explanation, line => line.Contains("mié 4 nov") && line.Contains("sin cupo (14 de 14)"));
    }

    [Fact]
    public void Non_working_days_are_skipped()
    {
        var result = AgendaCalculator.FindAvailability(Query(Day(2), Zona3, nonWorking: [Day(4)]));

        Assert.Equal(Day(7), result.Date);
        Assert.Contains(result.Explanation, line => line.Contains("mié 4 nov") && line.Contains("día inhábil"));
        Assert.DoesNotContain(result.Days, d => d.Date == Day(4));
    }

    [Fact]
    public void Fixed_paradas_take_cupo_traspaso_K_leaves_11_places()
    {
        // Ruta K with Traspaso K (3 fixed paradas) and 10 invitados: the next one takes place 14, and then it is full.
        var almostFull = AgendaCalculator.FindAvailability(Query(Day(3), MondayToSaturday, new() { [Day(3)] = 3 + 10 }));
        Assert.Equal(Day(3), almostFull.Date);
        Assert.Equal(14, almostFull.Place);

        var full = AgendaCalculator.FindAvailability(Query(Day(3), MondayToSaturday, new() { [Day(3)] = 3 + 11 }));
        Assert.Equal(Day(4), full.Date);
    }

    [Fact]
    public void The_cupo_of_the_ruta_is_used()
    {
        // A ruta with its own Ruta.CupoParadas of 10.
        var result = AgendaCalculator.FindAvailability(Query(Day(3), MondayToSaturday, new() { [Day(3)] = 10 }, cupo: 10));
        Assert.Equal(Day(4), result.Date);
        Assert.Equal(10, result.Cupo);
    }

    [Fact]
    public void Other_visit_days_are_listed_with_their_occupancy()
    {
        var result = AgendaCalculator.FindAvailability(
            Query(Day(2), Zona3, new() { [Day(4)] = 14, [Day(7)] = 8, [Day(11)] = 2 }, otherDays: 2));

        // Chosen day first, then the next 2 visit days. The full day before it is not listed.
        Assert.Equal(
            [new AgendaDay(Day(7), 8, 14, true), new AgendaDay(Day(11), 2, 14, true), new AgendaDay(Day(14), 0, 14, true)],
            result.Days);
    }

    [Fact]
    public void A_full_day_after_the_chosen_one_is_listed_as_not_available()
    {
        var result = AgendaCalculator.FindAvailability(Query(Day(2), Zona3, new() { [Day(7)] = 14 }, otherDays: 1));
        Assert.Equal([new AgendaDay(Day(4), 0, 14, true), new AgendaDay(Day(7), 14, 14, false)], result.Days);
    }

    [Fact]
    public void Without_cupo_in_the_search_range_there_is_no_date()
    {
        var everyDayFull = Enumerable.Range(0, 120).ToDictionary(i => Day(2).AddDays(i), _ => 14);
        var result = AgendaCalculator.FindAvailability(Query(Day(2), Zona3, everyDayFull));

        Assert.Null(result.Date);
        Assert.Null(result.Place);
        Assert.Empty(result.Days);
    }

    [Fact]
    public void A_zona_without_visit_days_cannot_be_scheduled()
    {
        Assert.Throws<DomainException>(() => AgendaCalculator.FindAvailability(Query(Day(2), [])));
    }

    [Fact]
    public void The_explanation_lists_the_visit_days_starting_on_monday()
    {
        var result = AgendaCalculator.FindAvailability(Query(Day(2), [DayOfWeek.Saturday, DayOfWeek.Wednesday]));
        Assert.Equal("Días de visita de la zona: miércoles y sábado.", result.Explanation[0]);
    }
}
