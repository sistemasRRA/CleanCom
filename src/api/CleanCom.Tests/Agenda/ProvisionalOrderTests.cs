using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Agenda;

/// <summary>
/// Order of the chofer's paradas until the ruta is optimized: morning, afternoon, then "any time".
/// </summary>
public class ProvisionalOrderTests
{
    private static readonly TimeOnly MorningEnd = new(13, 0);

    [Fact]
    public void Morning_goes_before_afternoon_and_any_time_last()
    {
        var ranks = new[] { Turno.CualquierHora, Turno.Tarde, Turno.Manana }
            .OrderBy(t => ProvisionalOrder.Rank(t, null, MorningEnd))
            .ToList();
        Assert.Equal([Turno.Manana, Turno.Tarde, Turno.CualquierHora], ranks);
    }

    [Fact]
    public void A_specific_time_goes_with_the_turno_it_falls_in()
    {
        Assert.Equal(0, ProvisionalOrder.Rank(Turno.HoraEspecifica, new TimeOnly(10, 30), MorningEnd));
        Assert.Equal(1, ProvisionalOrder.Rank(Turno.HoraEspecifica, new TimeOnly(13, 0), MorningEnd));
        Assert.Equal(1, ProvisionalOrder.Rank(Turno.HoraEspecifica, new TimeOnly(16, 0), MorningEnd));
    }

    [Fact]
    public void The_end_of_the_morning_comes_from_the_setting()
    {
        // With Turno.Manana changed to 09:00-12:00, 12:30 is already afternoon.
        Assert.Equal(1, ProvisionalOrder.Rank(Turno.HoraEspecifica, new TimeOnly(12, 30), new TimeOnly(12, 0)));
    }
}
