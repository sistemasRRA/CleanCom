using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.Chofer;

public record ChoferRutaInfoDto(int Id, string Code, string Name, string Color);

/// <summary>A beneficio the invitado applied to this service: the chofer tells planta, and the POS applies the discount.</summary>
public record ChoferBeneficioDto(string Code, string Name, string Mechanic, string? Condition, bool Used);

/// <param name="Order">1, 2, 3… in the order to visit. Provisional (by turno) until the ruta is optimized.</param>
/// <param name="InvitadoName">The invitado, or the name of the parada fija or lugar.</param>
/// <param name="Address">Street, number, interior and colonia in one line.</param>
/// <param name="Activity">What is done there (paradas fijas).</param>
/// <param name="Viaje">"Traspaso K · paso 2 de 3" when it is part of a viaje.</param>
/// <param name="EstimatedArrival">Estimated time from the ruta plan.</param>
/// <param name="ArrivedOutsideRadius">Llegué was confirmed farther than Gps.ToleranciaMetros.</param>
public record ChoferParadaDto(
    int Id,
    int Order,
    ParadaKind Kind,
    ParadaCategory? Category,
    ParadaState State,
    Turno Turno,
    TimeOnly? SpecificTime,
    TimeOnly? Before,
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
    string? Notes,
    string? Activity,
    string? Viaje,
    TimeOnly? EstimatedArrival,
    DateTimeOffset? EnCaminoAt,
    DateTimeOffset? ArrivedAt,
    int? ArrivedDistanceMeters,
    bool ArrivedOutsideRadius,
    DateTimeOffset? ClosedAt = null,
    int Photos = 0,
    bool Signed = false,
    IReadOnlyList<ChoferBeneficioDto>? Beneficios = null);

/// <summary>
/// Answer of <c>GET /chofer/ruta</c>: what the signed-in chofer does on a day.
/// </summary>
/// <param name="Ruta">The ruta the office assigned that day, or the operador's usual ruta. Null when there is none.</param>
/// <param name="AssignedByOffice">True when the office assigned this operador to that day; false when it is the usual ruta.</param>
/// <param name="Notice">Why there is nothing to show (no operador, no ruta, day off), in Spanish.</param>
/// <param name="IsToday">En camino and Llegué are only for today's ruta.</param>
/// <param name="NextParadaId">The parada to go to next (first pending in order); null when none or not today.</param>
/// <param name="OrderRequired">Visita.OrdenObligatorio: only the next parada can be started.</param>
/// <param name="PhotoRequired">Visita.FotoObligatoria: a visit is closed (done or not) only with a photo.</param>
/// <param name="ToleranceMeters">Gps.ToleranciaMetros: Llegué is suggested inside this radius.</param>
/// <param name="Shift">Inicio and fin de turno of that day; null when the chofer has no ruta.</param>
public record ChoferRutaDto(
    DateOnly Date,
    string Label,
    string? OperadorName,
    ChoferRutaInfoDto? Ruta,
    bool AssignedByOffice,
    string? Notice,
    IReadOnlyList<ChoferParadaDto> Paradas,
    bool IsToday = false,
    int? NextParadaId = null,
    bool OrderRequired = true,
    int ToleranceMeters = 300,
    bool PhotoRequired = true,
    string? Message = null,
    ChoferShiftDto? Shift = null);
