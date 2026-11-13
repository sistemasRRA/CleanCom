using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.ParadasFijas;

/// <summary>One paso of a parada fija on a day: a vuelta shows one item per tramo, as the office is used to.</summary>
/// <param name="Step">1, 2, 3… within its vuelta; null for a single parada.</param>
/// <param name="Place">The lugar or the address of the paso.</param>
/// <param name="Kind">Qué se hace in this paso.</param>
/// <param name="Turno">Turno of this paso.</param>
/// <param name="Generated">Its paradas already exist in the ruta that day.</param>
public record FijasWeekItemDto(
    int ParadaFijaId,
    string Name,
    int? Step,
    int Steps,
    string? Place,
    string? Activity,
    ParadaCategory Category,
    ParadaKind Kind,
    Turno Turno,
    TimeOnly? Before,
    ParadaPlacement Placement,
    bool Generated);
