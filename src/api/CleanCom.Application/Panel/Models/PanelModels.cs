using CleanCom.Application.Agenda;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.Panel;

/// <summary>Answer of <c>GET /panel</c>: the day's operation at a glance (docs/pantallas/oficina/panel-del-dia.md).</summary>
/// <param name="IsToday">Delay, GPS and live positions only apply to today.</param>
/// <param name="FleetWithPending">Unidades with something to act on; the detail is in Unidades.</param>
public record PanelDto(
    DateOnly Date,
    string Label,
    bool IsToday,
    string? NonWorkingReason,
    TimeOnly Departure,
    TimeOnly MaxReturn,
    PanelIndicatorsDto Indicators,
    PanelPendingDto Pending,
    int FleetWithPending,
    IReadOnlyList<PanelRutaDto> Rutas,
    PanelLoadDto Load,
    IReadOnlyList<PanelVisitDto> LatestVisits,
    double BaseLat,
    double BaseLng,
    IReadOnlyList<PanelChoiceDto> Operadores,
    IReadOnlyList<PanelChoiceDto> Unidades);

/// <summary>An operador or unidad to choose in a ruta card.</summary>
/// <param name="Note">Why to think twice (a unidad with something pending), in Spanish.</param>
public record PanelChoiceDto(int Id, string Name, string? Note = null);

/// <param name="OnTimePercent">Arrivals within ± Parada.ToleranciaLlegadaMinutos of their estimate; null without arrivals.</param>
/// <param name="Collected">Reference payments declared by the choferes that day.</param>
/// <param name="InPlanta">Pedidos in planta now (en planta or listo, not yet out for entrega).</param>
public record PanelIndicatorsDto(
    int Recolecciones,
    int RecoleccionesPlanned,
    int Entregas,
    int EntregasPlanned,
    decimal Collected,
    int? OnTimePercent,
    int InPlanta);

/// <summary>Pending tray: each list is exactly what its counter counts.</summary>
/// <param name="PastOpen">Paradas of past days still open ("Reprogramar").</param>
/// <param name="ReadyWithoutEntrega">Pedidos 100% listos without an entrega date ("Confirmar entregas").</param>
/// <param name="LongInPlanta">Pedidos with Planta.DiasProceso days or more in planta ("Ver planta").</param>
public record PanelPendingDto(
    IReadOnlyList<PendingParadaDto> PastOpen,
    IReadOnlyList<PendingPedidoDto> ReadyWithoutEntrega,
    IReadOnlyList<PendingPedidoDto> LongInPlanta);

public record PendingParadaDto(int ParadaId, int? PedidoId, DateOnly Date, string RutaCode, string? Name, ParadaKind Kind, ParadaState State);

public record PendingPedidoDto(int PedidoId, string InvitadoName, PedidoEtapa Etapa, DateTimeOffset? Since, int Days);

/// <summary>A ruta's card.</summary>
/// <param name="Progress">Sin salir, en ruta or de regreso.</param>
/// <param name="LastLocationAt">Last GPS reading of the chofer that day.</param>
/// <param name="LateMinutes">Behind schedule by more than the tolerance (today only).</param>
/// <param name="Alerts">Atraso, horas extra, sin GPS and the plan's own alerts, in Spanish.</param>
public record PanelRutaDto(
    int RutaId,
    string Code,
    string Name,
    string Color,
    int? RutaDiaId,
    IReadOnlyList<string> Zonas,
    int? OperadorId,
    string? OperadorName,
    bool OperadorIsUsual,
    int? UnidadId,
    string? UnidadPlate,
    int Recolecciones,
    int RecoleccionesPlanned,
    int Goal,
    int Entregas,
    int EntregasPlanned,
    int Paradas,
    int Closed,
    RutaProgress Progress,
    double Km,
    TimeOnly ReturnAt,
    TimeOnly MaxReturn,
    DateTimeOffset? LastLocationAt,
    int? LateMinutes,
    decimal Collected,
    IReadOnlyList<string> Alerts);

/// <summary>Next 7 days: paradas that take cupo (fixed ones included) against each ruta's cupo.</summary>
public record PanelLoadDto(IReadOnlyList<BoardRutaDto> Rutas, IReadOnlyList<BoardDayDto> Days);

public record PanelVisitDto(int ParadaId, int? PedidoId, string RutaCode, string? Name, ParadaKind Kind, ParadaState State,
    DateTimeOffset ClosedAt, int Photos, bool Signed);
