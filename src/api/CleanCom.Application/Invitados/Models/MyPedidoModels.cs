using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.Invitados;

/// <summary>A pedido of the signed-in invitado ("Mis pedidos").</summary>
/// <param name="Address">The address in one line ("Casa · Calz. de Tlalpan 1200, Portales").</param>
public record MyPedidoDto(
    int Id,
    PedidoEtapa Etapa,
    string EtapaName,
    DateOnly RecoleccionDate,
    DateOnly? EntregaDate,
    Turno Turno,
    IReadOnlyList<string> LineasServicio,
    int? GarmentsApprox,
    string Address,
    PedidoOrigin Origin,
    DateTimeOffset RegisteredAt);

/// <param name="At">When it reached that etapa; null while it has not.</param>
/// <param name="Current">The etapa the pedido is in now.</param>
public record MyPedidoStepDto(PedidoEtapa Etapa, string Name, DateTimeOffset? At, bool Done, bool Current);

/// <summary>Seguimiento: the pedido, its etapas, the visit of the day and the chofer while on the way.</summary>
/// <param name="VisitState">State of today's recolección or entrega (pendiente, en camino, en sitio…); null when there is none today.</param>
/// <param name="Chofer">Only while today's visit is en camino; null otherwise.</param>
/// <param name="CanChangeDay">The recolección is still to be done (Agendado, chofer not on the way): "Cambiar día" is offered.</param>
/// <param name="CanCancel">Same, and before Agenda.CorteCancelacion of the day before; afterwards only the office cancels.</param>
/// <param name="CancelUntil">Last instant to cancel from the app, while the recolección is still to be done.</param>
public record MySeguimientoDto(
    MyPedidoDto Pedido,
    IReadOnlyList<MyPedidoStepDto> Steps,
    ParadaState? VisitState,
    MyChoferDto? Chofer = null,
    bool CanChangeDay = false,
    bool CanCancel = false,
    DateTimeOffset? CancelUntil = null);

/// <summary>"Cambiar día": the new date, one of the days of GET /yo/pedidos/{id}/cambiar-dia.</summary>
public record ChangeDayRequest(DateOnly Date);

/// <summary>"Cancelar": optionally why; it goes to the pedido's history.</summary>
public record CancelPedidoRequest(string? Note);

/// <summary>The chofer on the way to the invitado's address.</summary>
/// <param name="Name">First name only.</param>
/// <param name="Lat">Last known position; null until the chofer's phone sends one.</param>
/// <param name="At">When that position was taken.</param>
/// <param name="SignalLostAt">From then on the position is old (Gps.SinSenalMinutos without a new one).</param>
/// <param name="DestinationLat">The invitado's address on the map; null when it has no coordinates.</param>
/// <param name="EstimatedArrival">From the last position (Ruta.VelocidadPromedio and Ruta.FactorDistancia); null without position or address.</param>
public record MyChoferDto(
    string? Name,
    string RutaCode,
    string? RutaColor,
    double? Lat,
    double? Lng,
    DateTimeOffset? At,
    DateTimeOffset? SignalLostAt,
    double? DestinationLat,
    double? DestinationLng,
    DateTimeOffset? EstimatedArrival);
