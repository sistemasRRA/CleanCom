using CleanCom.Api.Common;
using CleanCom.Application.Crm;
using CleanCom.Domain.Access;
using CleanCom.Domain.Crm;
using CleanCom.Domain.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// Clientes (CRM): status, "Llamar hoy", ficha, contacts and follow-up (docs/pantallas/oficina/clientes-crm.md).
/// </summary>
[ApiController]
[Route("invitados")]
[Tags("Clientes (CRM)")]
public class CrmController(CrmService service, CrmInvitadoService profile) : ControllerBase
{
    /// <summary>
    /// Invitados with their calculated status. Filters: status, ruta, text (name, colonia or phone), type of invitado
    /// and state of the app account.
    /// </summary>
    [HttpGet]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.View)]
    public Task<CrmListDto> List(
        [FromQuery(Name = "estatus")] CrmStatus? status,
        [FromQuery(Name = "ruta")] int? rutaId,
        [FromQuery] string? q,
        CancellationToken ct,
        [FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "porPagina")] int pageSize = CrmService.DefaultPageSize,
        [FromQuery(Name = "tipo")] InvitadoType? tipo = null,
        [FromQuery(Name = "cuenta")] AppAccountState? cuenta = null) =>
        service.ListAsync(false, status, rutaId, q, ct, page, pageSize, tipo, cuenta);

    /// <summary>Pedidos, visits, tickets, avisos and contacts of the invitado in one list, newest first.</summary>
    [HttpGet("{id:int}/historia")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.View)]
    public Task<IReadOnlyList<HistoriaItemDto>> History(int id, CancellationToken ct) =>
        profile.HistoryAsync(id, ct);

    /// <summary>Other active invitados who share a phone with this one.</summary>
    [HttpGet("{id:int}/duplicados")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.View)]
    public Task<IReadOnlyList<DuplicateDto>> Duplicates(int id, CancellationToken ct) =>
        profile.DuplicatesAsync(id, ct);

    /// <summary>
    /// Merges a duplicate into this invitado (pedidos, addresses, contacts, beneficios, avisos, app account) and gives
    /// the duplicate a baja lógica. It cannot be undone: SuperUser and Administración only.
    /// </summary>
    [HttpPost("{id:int}/fusionar")]
    [Authorize(Policies.Administrators)]
    public async Task<CrmFichaDto> Merge(int id, MergeRequest request, CancellationToken ct)
    {
        await profile.MergeAsync(id, request, ct);
        return await service.FichaAsync(id, ct);
    }

    /// <summary>The WhatsApp invitation to the app (text of AppInvitado.MensajeInvitacion); records when it was sent.</summary>
    [HttpPost("{id:int}/invitar-app")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.Edit)]
    public Task<AppInvitationDto> InviteToApp(int id, CancellationToken ct) =>
        profile.InviteAsync(id, ct);

    /// <summary>Blocks or gives back the access to the app; blocking ends the open sessions.</summary>
    [HttpPost("{id:int}/bloquear-app")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.Edit)]
    public async Task<CrmFichaDto> BlockApp(int id, BlockAppRequest request, CancellationToken ct)
    {
        await profile.SetBlockedAsync(id, request, ct);
        return await service.FichaAsync(id, ct);
    }

    /// <summary>Le toca or en riesgo whose ruta comes tomorrow, without a future contact date and not "do not contact".</summary>
    [HttpGet("llamar-hoy")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.View)]
    public Task<CrmListDto> CallToday(
        [FromQuery(Name = "ruta")] int? rutaId,
        [FromQuery] string? q,
        CancellationToken ct,
        [FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "porPagina")] int pageSize = CrmService.DefaultPageSize) =>
        service.ListAsync(true, null, rutaId, q, ct, page, pageSize);

    [HttpGet("{id:int}/ficha")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.View)]
    public Task<CrmFichaDto> Ficha(int id, CancellationToken ct) =>
        service.FichaAsync(id, ct);

    /// <summary>Records a call or WhatsApp; without a date the next contact follows the result.</summary>
    [HttpPost("{id:int}/contactos")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.Edit)]
    public Task<CrmFichaDto> AddContact(int id, SaveContactRequest request, CancellationToken ct) =>
        service.AddContactAsync(id, request, ct);

    /// <summary>Manual status, fixed frequency, next contact and "do not contact".</summary>
    [HttpPut("{id:int}/seguimiento")]
    [RequireScreen(Screens.ClientesCrm, AccessLevel.Edit)]
    public Task<CrmFichaDto> UpdateSeguimiento(int id, SaveSeguimientoRequest request, CancellationToken ct) =>
        service.UpdateSeguimientoAsync(id, request, ct);
}
