using CleanCom.Domain.Pedidos;

namespace CleanCom.Domain.Routing;

/// <summary>What <see cref="ProvisionalOrder.Sort{T}"/> needs to know about a parada.</summary>
/// <param name="Position">Order in the optimized ruta; 0 while it has not been optimized.</param>
public readonly record struct OrderKey(
    int Position,
    ParadaPlacement Placement,
    Turno Turno,
    TimeOnly? SpecificTime,
    bool Priority,
    int? ViajeId,
    int? ViajeStep,
    int Id);

/// <summary>
/// Order of the paradas until Google optimizes the ruta: the ones pinned first, then morning, afternoon and "any time"
/// last so it fills the gaps, and the ones pinned last at the end. A specific time goes with the turno it falls in,
/// by hour. The pasos of a viaje keep their order (paso 1 before paso 2) even when their turnos differ. The end of the
/// morning comes from the Turno.Manana setting.
/// </summary>
public static class ProvisionalOrder
{
    public static int Rank(Turno turno, TimeOnly? specificTime, TimeOnly morningEnd) => turno switch
    {
        Turno.Manana => 0,
        Turno.HoraEspecifica => specificTime is { } t && t < morningEnd ? 0 : 1,
        Turno.Tarde => 1,
        _ => 2,
    };

    public static int PlacementRank(ParadaPlacement placement) => placement switch
    {
        ParadaPlacement.Primera => 0,
        ParadaPlacement.Ultima => 2,
        _ => 1,
    };

    /// <summary>Optimized paradas keep their position; the rest follow the provisional order after them.</summary>
    public static IReadOnlyList<T> Sort<T>(IEnumerable<T> items, Func<T, OrderKey> key, TimeOnly morningEnd)
    {
        var sorted = items.Select(item => (Item: item, Key: key(item)))
            .OrderBy(x => x.Key.Position == 0 ? int.MaxValue : x.Key.Position)
            .ThenBy(x => PlacementRank(x.Key.Placement))
            .ThenBy(x => Rank(x.Key.Turno, x.Key.SpecificTime, morningEnd))
            .ThenBy(x => x.Key.SpecificTime ?? TimeOnly.MaxValue)
            .ThenByDescending(x => x.Key.Priority)
            // A viaje sorts by its id as one block, its pasos by step.
            .ThenBy(x => x.Key.ViajeId is null ? 0 : 1)
            .ThenBy(x => x.Key.ViajeId ?? x.Key.Id)
            .ThenBy(x => x.Key.ViajeStep ?? 0)
            .ToList();
        return KeepViajeOrder(sorted, x => x.Key.ViajeId, x => x.Key.ViajeStep).Select(x => x.Item).ToList();
    }

    /// <summary>The places a viaje's pasos got are refilled with its pasos in step order.</summary>
    public static List<T> KeepViajeOrder<T>(IReadOnlyList<T> list, Func<T, int?> viajeId, Func<T, int?> step)
    {
        var result = list.ToList();
        foreach (var viaje in list.Where(x => viajeId(x) is not null).GroupBy(x => viajeId(x)!.Value))
        {
            var slots = result.Select((x, i) => (x, i)).Where(t => viajeId(t.x) == viaje.Key).Select(t => t.i).ToList();
            var inOrder = viaje.OrderBy(x => step(x) ?? 0).ToList();
            for (var i = 0; i < slots.Count; i++)
            {
                result[slots[i]] = inOrder[i];
            }
        }
        return result;
    }
}
