using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Routing;

/// <summary>
/// Provisional order of a whole ruta: pinned first and last, turnos, and viajes kept together and in order.
/// </summary>
public class ProvisionalSortTests
{
    private static readonly TimeOnly MorningEnd = new(13, 0);

    private record P(int Id, ParadaPlacement Placement = ParadaPlacement.Automatica, Turno Turno = Turno.Manana,
        int? ViajeId = null, int? Step = null, int Position = 0);

    private static OrderKey Key(P p) => new(p.Position, p.Placement, p.Turno, null, false, p.ViajeId, p.Step, p.Id);

    private static IEnumerable<int> Sort(params P[] paradas) => ProvisionalOrder.Sort(paradas, Key, MorningEnd).Select(p => p.Id);

    [Fact]
    public void Pinned_first_and_last_wrap_the_turnos()
    {
        Assert.Equal([3, 4, 2, 1], Sort(
            new P(1, ParadaPlacement.Ultima),
            new P(2, Turno: Turno.Tarde),
            new P(3, ParadaPlacement.Primera, Turno.Tarde),
            new P(4)));
    }

    [Fact]
    public void The_pasos_of_a_viaje_stay_together_and_in_order()
    {
        Assert.Equal([11, 13, 12, 14, 10], Sort(
            new P(10, ViajeId: 5, Step: 3),
            new P(11),
            new P(12, ViajeId: 5, Step: 1),
            new P(13),
            new P(14, ViajeId: 5, Step: 2)));
    }

    [Fact]
    public void An_optimized_position_wins_over_the_provisional_order()
    {
        Assert.Equal([2, 1], Sort(new P(1, ParadaPlacement.Primera), new P(2, Turno: Turno.Tarde, Position: 1)));
    }
}
