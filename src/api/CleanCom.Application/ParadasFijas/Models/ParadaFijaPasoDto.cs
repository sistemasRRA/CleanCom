using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.ParadasFijas;

/// <param name="Kind">Null = the parada fija's.</param>
/// <param name="Turno">Null = the parada fija's.</param>
public record ParadaFijaPasoDto(
    int Order,
    int? LugarId,
    string? LugarName,
    string? Address,
    double? Lat,
    double? Lng,
    ParadaKind? Kind,
    Turno? Turno,
    TimeOnly? Before,
    string? Activity,
    string? Notes);
