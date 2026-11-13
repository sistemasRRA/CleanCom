using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.ParadasFijas;

/// <param name="Generated">Its paradas already exist in the ruta that day.</param>
public record FijasWeekItemDto(int ParadaFijaId, string Name, ParadaCategory Category, ParadaKind Kind, Turno Turno, ParadaPlacement Placement, int Pasos, bool Generated);
