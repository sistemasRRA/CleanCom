using CleanCom.Api.Common;
using CleanCom.Application.Fleet;
using CleanCom.Domain.Access;
using CleanCom.Domain.Fleet;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Fleet: vehicles, documents, logbook, recurring payments and notices (docs/pantallas/oficina/unidades.md).</summary>
[ApiController]
[Tags("Unidades")]
public class UnidadesController(UnidadService service) : ControllerBase
{
    /// <summary>Unidades with their alerts, service and the year's spending, and how many have something pending.</summary>
    [HttpGet("unidades")]
    [RequireScreen(Screens.Unidades, AccessLevel.View)]
    public Task<UnidadesDto> List([FromQuery] bool? includeInactive, CancellationToken ct) =>
        service.ListAsync(includeInactive ?? false, ct);

    [HttpGet("unidades/{id:int}")]
    [RequireScreen(Screens.Unidades, AccessLevel.View)]
    public Task<UnidadDetailDto> Get(int id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost("unidades")]
    [RequireScreen(Screens.Unidades, AccessLevel.Edit)]
    [ProducesResponseType<UnidadDetailDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveUnidadRequest request, CancellationToken ct)
    {
        var unidad = await service.SaveAsync(null, request, ct);
        return Created($"/unidades/{unidad.Card.Id}", unidad);
    }

    /// <summary>Data, state (fuera de servicio) and baja lógica (active: false).</summary>
    [HttpPut("unidades/{id:int}")]
    [RequireScreen(Screens.Unidades, AccessLevel.Edit)]
    public Task<UnidadDetailDto> Update(int id, SaveUnidadRequest request, CancellationToken ct) =>
        service.SaveAsync(id, request, ct);

    /// <summary>Logbook entry: servicio, reparación, falla, pago (of a recurring payment or not) or kilometraje.</summary>
    [HttpPost("unidades/{id:int}/eventos")]
    [RequireScreen(Screens.Unidades, AccessLevel.Edit)]
    public Task<UnidadDetailDto> AddEvent(int id, SaveUnidadEventRequest request, CancellationToken ct) =>
        service.AddEventAsync(id, request, ct);

    [HttpPost("unidades/{id:int}/eventos/{eventoId:int}/resolver")]
    [RequireScreen(Screens.Unidades, AccessLevel.Edit)]
    public Task<UnidadDetailDto> ResolveFailure(int id, int eventoId, CancellationToken ct) =>
        service.ResolveFailureAsync(id, eventoId, ct);

    /// <summary>
    /// Póliza, tarjeta de circulación, verificación (validFrom = the day it was done) or receipt. The file is optional:
    /// PDF, JPG, PNG or WebP up to 15 MB.
    /// </summary>
    [HttpPost("unidades/{id:int}/documentos")]
    [RequireScreen(Screens.Unidades, AccessLevel.Edit)]
    [RequestSizeLimit(UnidadService.MaxFileBytes + 64 * 1024)]
    public async Task<UnidadDetailDto> AddDocument(int id, [FromForm] UnidadDocumentUpload form, CancellationToken ct)
    {
        await using var content = form.File?.OpenReadStream();
        return await service.AddDocumentAsync(id, form.Kind, form.ValidFrom, form.ValidUntil, form.Description, form.EventId,
            content, form.File?.Length ?? 0, ct);
    }

    [HttpGet("unidades/{id:int}/documentos/{documentoId:int}/archivo")]
    [RequireScreen(Screens.Unidades, AccessLevel.View)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DocumentFile(int id, int documentoId, CancellationToken ct)
    {
        var (content, type) = await service.OpenDocumentAsync(id, documentoId, ct);
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'";
        return File(content, type);
    }

    /// <summary>Baja lógica of a document.</summary>
    [HttpDelete("unidades/{id:int}/documentos/{documentoId:int}")]
    [RequireScreen(Screens.Unidades, AccessLevel.Edit)]
    public Task<UnidadDetailDto> DeactivateDocument(int id, int documentoId, CancellationToken ct) =>
        service.DeactivateDocumentAsync(id, documentoId, ct);

    /// <summary>Recurring payment: tenencia or refrendo, credit or lease, insurance in installments.</summary>
    [HttpPost("unidades/{id:int}/pagos")]
    [RequireScreen(Screens.Unidades, AccessLevel.Edit)]
    public Task<UnidadDetailDto> AddPayment(int id, SaveRecurringPaymentRequest request, CancellationToken ct) =>
        service.SavePaymentAsync(id, null, request, ct);

    [HttpPut("unidades/{id:int}/pagos/{pagoId:int}")]
    [RequireScreen(Screens.Unidades, AccessLevel.Edit)]
    public Task<UnidadDetailDto> UpdatePayment(int id, int pagoId, SaveRecurringPaymentRequest request, CancellationToken ct) =>
        service.SavePaymentAsync(id, pagoId, request, ct);

    /// <summary>When and to whom each fleet notice goes.</summary>
    [HttpGet("avisos-flota/reglas")]
    [RequireScreen(Screens.Unidades, AccessLevel.View)]
    public Task<IReadOnlyList<FleetAlertRuleDto>> Rules(CancellationToken ct) => service.RulesAsync(ct);

    [HttpPut("avisos-flota/reglas")]
    [RequireScreen(Screens.Unidades, AccessLevel.Edit)]
    public Task<IReadOnlyList<FleetAlertRuleDto>> SaveRules(IReadOnlyList<FleetAlertRuleDto> rules, CancellationToken ct) =>
        service.SaveRulesAsync(rules, ct);
}

/// <summary>Multipart form of a unidad's document.</summary>
public class UnidadDocumentUpload
{
    public UnidadDocumentKind Kind { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public string? Description { get; set; }
    public int? EventId { get; set; }
    public IFormFile? File { get; set; }
}
