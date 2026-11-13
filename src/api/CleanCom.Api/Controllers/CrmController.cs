using CleanCom.Api.Common;
using CleanCom.Application.Crm;
using CleanCom.Domain.Access;
using CleanCom.Domain.Crm;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// Clientes (CRM): status, "Llamar hoy", ficha, contacts and follow-up (docs/pantallas/oficina/clientes-crm.md).
/// </summary>
[ApiController]
[Route("invitados")]
[Tags("Clientes (CRM)")]
public class CrmController(CrmService service) : ControllerBase
{
    /// <summary>Invitados with their calculated status. Filters: status, ruta and text (name, colonia or phone).</summary>
    [HttpGet]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.View)]
    public Task<CrmListDto> List([FromQuery(Name = "estatus")] CrmStatus? status, [FromQuery(Name = "ruta")] int? rutaId, [FromQuery] string? q, CancellationToken ct) =>
        service.ListAsync(false, status, rutaId, q, ct);

    /// <summary>Le toca or en riesgo whose ruta comes tomorrow, without a future contact date and not "do not contact".</summary>
    [HttpGet("llamar-hoy")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.View)]
    public Task<CrmListDto> CallToday([FromQuery(Name = "ruta")] int? rutaId, [FromQuery] string? q, CancellationToken ct) =>
        service.ListAsync(true, null, rutaId, q, ct);

    [HttpGet("{id:int}/ficha")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.View)]
    public Task<CrmFichaDto> Ficha(int id, CancellationToken ct) =>
        service.FichaAsync(id, ct);

    /// <summary>Records a call or WhatsApp; without a date the next contact follows the result.</summary>
    [HttpPost("{id:int}/contactos")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.Edit)]
    public Task<CrmFichaDto> AddContacto(int id, SaveContactoRequest request, CancellationToken ct) =>
        service.AddContactoAsync(id, request, ct);

    /// <summary>Manual status, fixed frequency, next contact and "do not contact".</summary>
    [HttpPut("{id:int}/seguimiento")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.Edit)]
    public Task<CrmFichaDto> UpdateSeguimiento(int id, SaveSeguimientoRequest request, CancellationToken ct) =>
        service.UpdateSeguimientoAsync(id, request, ct);
}
