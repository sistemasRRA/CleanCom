using CleanCom.Domain.Crm;

namespace CleanCom.Application.Crm;

/// <summary>Which invitados count: everyone except the tiendas internas, clientes finales, or convenios and empresas.</summary>
public enum ClientesTipo
{
    Todos,
    ClientesFinales,
    Convenios,
}

/// <summary>The indicators of a period.</summary>
/// <param name="Atendidos">Invitados with a service in the period (nuevos + recurrentes + recuperados).</param>
/// <param name="ActivosAlCierre">Invitados with a service in the Crm.DiasInactivo days before the close of the period.</param>
/// <param name="Pedidos">Services (pedidos not cancelled) in the period.</param>
/// <param name="Venta">Amount of the tickets (POS) of the period.</param>
/// <param name="VentaNuevos">Amount of the tickets of the nuevos.</param>
/// <param name="TicketPromedio">Venta between the tickets with an amount; null without tickets.</param>
public record ClientesTotalsDto(
    int Nuevos,
    int Recurrentes,
    int Recuperados,
    int Perdidos,
    int Atendidos,
    int ActivosAlCierre,
    int Pedidos,
    decimal Venta,
    decimal VentaNuevos,
    decimal? TicketPromedio);

/// <summary>A source (cómo nos conoció) in the period.</summary>
/// <param name="PorcentajeVenta">Share of the period's venta, 0 to 100; null when there was no venta.</param>
public record FuenteRowDto(
    string Fuente,
    int Nuevos,
    decimal VentaNuevos,
    int Atendidos,
    int Pedidos,
    decimal Venta,
    decimal? TicketPromedio,
    decimal? PorcentajeVenta,
    int Perdidos);

/// <summary>One of the last periods (trend).</summary>
public record ClientesTrendDto(string Label, DateOnly Start, DateOnly End, bool IsPartial, ClientesTotalsDto Totals);

/// <summary>Answer of <c>GET /gerencia/clientes</c>.</summary>
/// <param name="IsPartial">The period is still open: counted up to today and compared with the same days of the previous one.</param>
/// <param name="ComparedLabel">What it is compared with ("1 al 26 de octubre 2026").</param>
/// <param name="InactiveDays">Crm.DiasInactivo: the days of the definitions (perdido, recuperado, activo).</param>
public record ClientesFuentesDto(
    ClientesPeriod Periodo,
    ClientesTipo Tipo,
    string Label,
    DateOnly Start,
    DateOnly End,
    bool IsPartial,
    string ComparedLabel,
    ClientesTotalsDto Totals,
    ClientesTotalsDto Previous,
    IReadOnlyList<FuenteRowDto> Fuentes,
    IReadOnlyList<ClientesTrendDto> Trend,
    int InactiveDays);

/// <summary>Invitados of one month of first service and how many ordered again.</summary>
/// <param name="Base30">New ones whose 30 days already passed (or who already repeated); the share is over them.</param>
/// <param name="Pct30">Repitieron30 / Base30 × 100, one decimal; null while no one can be measured.</param>
public record CohortDto(
    DateOnly Month,
    string Label,
    int Nuevos,
    int Base30,
    int Repitieron30,
    int Base60,
    int Repitieron60,
    int Base90,
    int Repitieron90,
    decimal? Pct30,
    decimal? Pct60,
    decimal? Pct90);

/// <summary>Answer of <c>GET /gerencia/clientes/retencion</c>: the last 6 months, newest first.</summary>
/// <param name="Fuentes">The sources with new invitados in those months, to filter by one.</param>
public record RetencionDto(ClientesTipo Tipo, string? Fuente, IReadOnlyList<CohortDto> Cohorts, IReadOnlyList<string> Fuentes);

/// <summary>An invitado who became perdido in the period.</summary>
/// <param name="LastService">Their last service before becoming perdido.</param>
/// <param name="Reactivation">Already sent to "Llamar hoy" and not contacted yet.</param>
public record PerdidoDto(
    int InvitadoId,
    string Name,
    string WhatsAppPhone,
    string Fuente,
    DateOnly LastService,
    DateOnly LostOn,
    int Services,
    decimal Venta,
    bool DoNotContact,
    bool Reactivation,
    bool CameBack);

/// <summary>Answer of <c>GET /gerencia/clientes/perdidos</c>.</summary>
public record PerdidosDto(string Label, IReadOnlyList<PerdidoDto> Rows);

/// <summary>Invitados to send to "Llamar hoy" (reactivation).</summary>
public record LlamarPerdidosRequest(IReadOnlyList<int> InvitadoIds);

/// <param name="Sent">Now in "Llamar hoy".</param>
/// <param name="Skipped">Not sent: asked not to be contacted or no longer active.</param>
public record LlamarPerdidosResultDto(int Sent, int Skipped, string Message);
