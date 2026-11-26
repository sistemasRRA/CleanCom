using CleanCom.Api.Common;
using CleanCom.Application.Chofer;
using CleanCom.Application.Common;
using CleanCom.Application.Evidencias;
using CleanCom.Domain.Access;
using CleanCom.Domain.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Evidencias y caja (docs/pantallas/oficina/evidencias-y-caja.md) and the files of the evidence.</summary>
[ApiController]
[Tags("Evidencias y caja")]
public class EvidenceController(EvidenceService files, EvidenciasService visits, CorteCajaService cortes) : ControllerBase
{
    /// <summary>The photo or signature of a visit.</summary>
    [HttpGet("evidencias/{id:int}/archivo")]
    [Authorize(Policies.SeesEvidencias)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> File(int id, CancellationToken ct)
    {
        var (content, type) = await files.OpenAsync(id, ct);
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'";
        Response.Headers.CacheControl = "private, max-age=86400";
        return File(content, type);
    }

    /// <summary>
    /// Closed visits (done or not) of a period (yyyy-MM-dd, today when empty; up to 92 days) with photos, signature,
    /// tickets and reference payment, newest first. Paged (pagina from 1, porPagina up to 200); totals cover every page.
    /// </summary>
    [HttpGet("visitas")]
    [RequireScreen(Screens.EvidenciasYCaja, AccessLevel.View)]
    public Task<VisitasDto> Visits(
        [FromQuery(Name = "desde")] DateOnly? desde,
        [FromQuery(Name = "hasta")] DateOnly? hasta,
        [FromQuery(Name = "ruta")] int? rutaId,
        [FromQuery(Name = "formaPago")] PaymentMethod? formaPago,
        [FromQuery(Name = "pagina")] int? pagina,
        [FromQuery(Name = "porPagina")] int? porPagina,
        CancellationToken ct) =>
        visits.ListAsync(new VisitasQuery(desde, hasta, rutaId, formaPago, pagina ?? 1, porPagina ?? 50), ct);

    /// <summary>The visits with the same filters as an Excel workbook (every page).</summary>
    [HttpGet("visitas/exportar")]
    [RequireScreen(Screens.EvidenciasYCaja, AccessLevel.View)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery(Name = "desde")] DateOnly? desde,
        [FromQuery(Name = "hasta")] DateOnly? hasta,
        [FromQuery(Name = "ruta")] int? rutaId,
        [FromQuery(Name = "formaPago")] PaymentMethod? formaPago,
        CancellationToken ct)
    {
        var (content, name) = await visits.ExportAsync(new VisitasQuery(desde, hasta, rutaId, formaPago), ct);
        return File(content, XlsxWriter.ContentType, name);
    }

    /// <summary>Galería del pedido: the photos and signature of each of its visits (recolección and entrega).</summary>
    [HttpGet("pedidos/{id:int}/evidencias")]
    [Authorize(Policies.SeesEvidencias)]
    public Task<PedidoEvidenceDto> PedidoGallery(int id, CancellationToken ct) =>
        visits.PedidoAsync(id, ct);

    /// <summary>
    /// Cortes de caja of a day (yyyy-MM-dd, today when empty): per ruta, what the visits recorded, what the chofer
    /// declared and the POS, with the differences.
    /// </summary>
    [HttpGet("cortes")]
    [RequireScreen(Screens.EvidenciasYCaja, AccessLevel.View)]
    public Task<CortesDto> Cortes([FromQuery(Name = "fecha")] DateOnly? fecha, CancellationToken ct) =>
        cortes.DayAsync(fecha, ct);

    /// <summary>Closes a corte with the POS amounts and a note (required with differences); it gets its folio.</summary>
    [HttpPost("cortes/{id:int}/cerrar")]
    [RequireScreen(Screens.EvidenciasYCaja, AccessLevel.Edit)]
    public Task<CortesDto> CloseCorte(int id, CloseCorteRequest request, CancellationToken ct) =>
        cortes.CloseAsync(id, request, ct);

    /// <summary>Reopens a closed corte with a reason (permiso especial "Reabrir un corte de caja cerrado").</summary>
    [HttpPost("cortes/{id:int}/reabrir")]
    [RequireScreen(Screens.ReabrirCortes, AccessLevel.Edit)]
    public Task<CortesDto> ReopenCorte(int id, ReopenCorteRequest request, CancellationToken ct) =>
        cortes.ReopenAsync(id, request, ct);

    /// <summary>
    /// Closes a chofer's open shift from the office (permiso especial "Cerrar el turno de un chofer desde oficina"):
    /// what they handed in, the final km when known and why. A long shift or paradas still open answer 409 until confirmed.
    /// </summary>
    [HttpPost("rutas/dia/{id:int}/turno/cerrar")]
    [RequireScreen(Screens.CerrarTurnos, AccessLevel.Edit)]
    public Task<CortesDto> CloseShift(int id, OfficeEndShiftRequest request, CancellationToken ct) =>
        cortes.CloseShiftAsync(id, request, ct);
}
