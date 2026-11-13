using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.Chofer;

public record ChoferRutaInfoDto(int Id, string Code, string Name, string Color);

/// <param name="Order">1, 2, 3… in the order to visit. Provisional (by turno) until the ruta is optimized in week 3.</param>
/// <param name="Address">Street, number, interior and colonia in one line.</param>
public record ChoferParadaDto(
    int Id,
    int Order,
    ParadaKind Kind,
    ParadaState State,
    Turno Turno,
    TimeOnly? SpecificTime,
    bool Priority,
    int? PedidoId,
    string? InvitadoName,
    string? Phone,
    string? Address,
    string? References,
    string? ZonaCode,
    double? Lat,
    double? Lng,
    int? GarmentsApprox,
    IReadOnlyList<string> LineasServicio,
    string? Notes);

/// <summary>
/// Answer of <c>GET /chofer/ruta</c>: what the signed-in chofer does on a day.
/// </summary>
/// <param name="Ruta">The ruta the office assigned that day, or the operador's usual ruta. Null when there is none.</param>
/// <param name="AssignedByOffice">True when the office assigned this operador to that day; false when it is the usual ruta.</param>
/// <param name="Notice">Why there is nothing to show (no operador, no ruta, day off), in Spanish.</param>
public record ChoferRutaDto(
    DateOnly Date,
    string Label,
    string? OperadorName,
    ChoferRutaInfoDto? Ruta,
    bool AssignedByOffice,
    string? Notice,
    IReadOnlyList<ChoferParadaDto> Paradas);
