using CleanCom.Api.Common;
using CleanCom.Application.Crm;
using CleanCom.Domain.Access;
using CleanCom.Domain.Crm;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Clientes y fuentes (docs/pantallas/oficina/clientes-y-fuentes.md).</summary>
[ApiController]
[Route("gerencia/clientes")]
[Tags("Clientes y fuentes")]
public class ClientesFuentesController(ClientesFuentesService service) : ControllerBase
{
    /// <summary>
    /// Nuevos, recurrentes, recuperados, perdidos, venta, atendidos and activos al cierre of the period that contains
    /// fecha (yyyy-MM-dd, today when empty), compared with the previous one (the same days while it is open), by source,
    /// and the last 6 periods.
    /// </summary>
    [HttpGet]
    [RequireScreen(Screens.ClientesYFuentes, AccessLevel.View)]
    public Task<ClientesFuentesDto> Period(
        [FromQuery(Name = "periodo")] ClientesPeriod? periodo,
        [FromQuery(Name = "fecha")] DateOnly? fecha,
        [FromQuery(Name = "tipo")] ClientesTipo? tipo,
        CancellationToken ct) =>
        service.PeriodAsync(periodo ?? ClientesPeriod.Mes, fecha, tipo ?? ClientesTipo.Todos, ct);

    /// <summary>The nuevos of each of the last 6 months and how many ordered again at 30, 60 and 90 days; optionally of one source.</summary>
    [HttpGet("retencion")]
    [RequireScreen(Screens.ClientesYFuentes, AccessLevel.View)]
    public Task<RetencionDto> Retention(
        [FromQuery(Name = "fecha")] DateOnly? fecha,
        [FromQuery(Name = "tipo")] ClientesTipo? tipo,
        [FromQuery(Name = "fuente")] string? fuente,
        CancellationToken ct) =>
        service.RetentionAsync(fecha, tipo ?? ClientesTipo.Todos, fuente, ct);

    /// <summary>The invitados who became perdidos in the period (more than Crm.DiasInactivo days without a service).</summary>
    [HttpGet("perdidos")]
    [RequireScreen(Screens.ClientesYFuentes, AccessLevel.View)]
    public Task<PerdidosDto> Lost(
        [FromQuery(Name = "periodo")] ClientesPeriod? periodo,
        [FromQuery(Name = "fecha")] DateOnly? fecha,
        [FromQuery(Name = "tipo")] ClientesTipo? tipo,
        [FromQuery(Name = "fuente")] string? fuente,
        CancellationToken ct) =>
        service.LostAsync(periodo ?? ClientesPeriod.Mes, fecha, tipo ?? ClientesTipo.Todos, fuente, ct);

    /// <summary>Sends invitados to "Llamar hoy" of the CRM (reactivation) until a contact is registered with them.</summary>
    [HttpPost("perdidos/llamar")]
    [RequireScreen(Screens.ClientesYFuentes, AccessLevel.Edit)]
    public Task<LlamarPerdidosResultDto> Call(LlamarPerdidosRequest request, CancellationToken ct) =>
        service.CallAsync(request, ct);
}
