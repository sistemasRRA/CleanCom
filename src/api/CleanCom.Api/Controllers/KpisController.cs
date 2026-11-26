using CleanCom.Api.Common;
using CleanCom.Application.Kpis;
using CleanCom.Domain.Access;
using CleanCom.Domain.Kpis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// KPIs with their goals per week, month or quarter (docs/pantallas/oficina/kpis.md). The values are saved by the
/// nightly job; a period nobody calculated yet is calculated on the first visit.
/// </summary>
[ApiController]
[Route("kpis")]
[Tags("KPIs")]
public class KpisController(KpiService service) : ControllerBase
{
    /// <summary>Every indicator of the period that contains <paramref name="fecha"/> (today when empty).</summary>
    [HttpGet]
    [RequireScreen(Screens.Kpis, AccessLevel.View)]
    public Task<KpiBoardDto> Board([FromQuery] KpiPeriod periodo = KpiPeriod.Semana, [FromQuery] DateOnly? fecha = null, CancellationToken ct = default) =>
        service.BoardAsync(periodo, fecha, ct);

    /// <summary>The visits, tickets, pedidos or invitados that form an indicator, optionally of one ruta.</summary>
    [HttpGet("{clave}/detalle")]
    [RequireScreen(Screens.Kpis, AccessLevel.View)]
    public Task<KpiDetailDto> Detail(string clave, [FromQuery] KpiPeriod periodo = KpiPeriod.Semana, [FromQuery] DateOnly? fecha = null,
        [FromQuery] int? rutaId = null, CancellationToken ct = default) =>
        service.DetailAsync(clave, periodo, fecha, rutaId, ct);

    /// <summary>The last weeks (8 by default) of every indicator with its light.</summary>
    [HttpGet("historico")]
    [RequireScreen(Screens.Kpis, AccessLevel.View)]
    public Task<KpiHistoryDto> History([FromQuery] int semanas = KpiService.DefaultHistoryWeeks, CancellationToken ct = default) =>
        service.HistoryAsync(semanas, ct);

    /// <summary>Calculates the period again now, with today's operations or corrected data.</summary>
    [HttpPost("recalcular")]
    [RequireScreen(Screens.Kpis, AccessLevel.Edit)]
    public Task<KpiBoardDto> Recalculate([FromQuery] KpiPeriod periodo = KpiPeriod.Semana, [FromQuery] DateOnly? fecha = null, CancellationToken ct = default) =>
        service.RecalculateAsync(periodo, fecha, ct);
}

/// <summary>Metas de los KPIs, edited in WhatsApp y metas (docs/pantallas/oficina/whatsapp-y-metas.md).</summary>
[ApiController]
[Route("metas")]
[Tags("KPIs")]
public class MetasController(KpiGoalService service) : ControllerBase
{
    /// <summary>Each indicator with its goal in force, the next one and its history, and the users who can answer for one.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.SeesMetas)]
    public Task<KpiGoalsDto> List(CancellationToken ct) => service.ListAsync(ct);

    /// <summary>Sets the goal from a date on (today when empty); past periods keep theirs.</summary>
    [HttpPut("{clave}")]
    [RequireScreen(Screens.WhatsAppYMetas, AccessLevel.Edit)]
    public Task<KpiGoalsDto> Save(string clave, SaveKpiGoalRequest request, CancellationToken ct) => service.SaveAsync(clave, request, ct);

    /// <summary>Removes a goal that has not started yet.</summary>
    [HttpDelete("{clave}/{id:int}")]
    [RequireScreen(Screens.WhatsAppYMetas, AccessLevel.Edit)]
    public Task<KpiGoalsDto> Deactivate(string clave, int id, CancellationToken ct) => service.DeactivateAsync(clave, id, ct);
}
