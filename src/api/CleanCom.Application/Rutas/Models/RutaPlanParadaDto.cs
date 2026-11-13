using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.Rutas;

/// <param name="Order">1, 2, 3… in the order the plan uses (optimized, or provisional by turno).</param>
/// <param name="Name">The invitado, or the parada fija and its lugar.</param>
/// <param name="WindowStart">Window from its turno (or around its specific time).</param>
/// <param name="Arrival">Estimated start of the visit, after waiting for the window.</param>
/// <param name="OutOfWindow">The estimated arrival is after its window closes.</param>
/// <param name="LunchAfter">The chofer's lunch goes right after this parada.</param>
/// <param name="Viaje">"Traspaso K · paso 2 de 3" when it is part of a viaje.</param>
public record RutaPlanParadaDto(
    int Id,
    int Order,
    ParadaKind Kind,
    ParadaCategory? Category,
    ParadaState State,
    Turno Turno,
    TimeOnly? SpecificTime,
    bool Priority,
    string? Name,
    string? Address,
    string? ZonaCode,
    double? Lat,
    double? Lng,
    TimeOnly WindowStart,
    TimeOnly WindowEnd,
    TimeOnly Arrival,
    TimeOnly Leave,
    int WaitMinutes,
    double KmFromPrevious,
    bool OutOfWindow,
    bool NoLocation,
    bool LunchAfter,
    ParadaPlacement Placement,
    bool PositionFixed,
    int? ViajeId,
    string? Viaje,
    int? PedidoId,
    int? ParadaFijaId,
    int? GarmentsApprox,
    string? Activity,
    string? Notes);
