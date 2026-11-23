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

/// <summary>Seguimiento: the pedido, its etapas and the visit of the day (the chofer on the map arrives in week 6).</summary>
/// <param name="VisitState">State of today's recolección or entrega (pendiente, en camino, en sitio…); null when there is none today.</param>
public record MySeguimientoDto(MyPedidoDto Pedido, IReadOnlyList<MyPedidoStepDto> Steps, ParadaState? VisitState);
