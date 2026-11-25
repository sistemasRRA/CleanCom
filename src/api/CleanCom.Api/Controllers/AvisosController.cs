using CleanCom.Api.Common;
using CleanCom.Application.Avisos;
using CleanCom.Domain.Access;
using CleanCom.Domain.Avisos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// Avisos to the invitados (docs/pantallas/oficina/whatsapp-y-metas.md): the log and the WhatsApp ones to send by hand
/// while WhatsApp is not connected.
/// </summary>
[ApiController]
[Route("avisos")]
[Tags("Avisos")]
public class AvisosController(AvisoService service) : ControllerBase
{
    /// <summary>The latest 200 avisos, newest first; filter by state (ParaMandarAMano = "Por mandar") or pedido.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.SeesAvisos)]
    public Task<AvisosDto> List([FromQuery] AvisoState? estado, [FromQuery] int? pedidoId, CancellationToken ct) =>
        service.ListAsync(estado, pedidoId, ct);

    /// <summary>"Ya lo mandé": a WhatsApp aviso sent by hand. Only one that is waiting to be sent by hand.</summary>
    [HttpPost("{id:int}/mandado")]
    [Authorize(Policy = Policies.SendsAvisos)]
    public Task<AvisoDto> SentByHand(int id, CancellationToken ct) => service.MarkSentByHandAsync(id, ct);
}

/// <summary>Plantillas por evento: WhatsApp text with variables, push text, Meta name and active.</summary>
[ApiController]
[Route("plantillas")]
[Tags("Avisos")]
public class PlantillasController(PlantillaService service) : ControllerBase
{
    [HttpGet]
    [RequireScreen(Screens.WhatsAppYMetas, AccessLevel.View)]
    public Task<IReadOnlyList<PlantillaDto>> List(CancellationToken ct) => service.ListAsync(ct);

    /// <summary>Applies to the avisos recorded from now on. An inactive template sends nothing for its event.</summary>
    [HttpPut("{id:int}")]
    [RequireScreen(Screens.WhatsAppYMetas, AccessLevel.Edit)]
    public Task<PlantillaDto> Update(int id, SavePlantillaRequest request, CancellationToken ct) => service.SaveAsync(id, request, ct);

    /// <summary>The texts sent (saved or not) filled in with sample data.</summary>
    [HttpPost("{id:int}/previsualizar")]
    [RequireScreen(Screens.WhatsAppYMetas, AccessLevel.View)]
    public Task<PlantillaPreviewDto> Preview(int id, SavePlantillaRequest request, CancellationToken ct) => service.PreviewAsync(id, request, ct);
}

/// <summary>The office bell: what the invitado did in the app (new pedido, day change, cancellation).</summary>
[ApiController]
[Route("avisos-oficina")]
[Tags("Avisos")]
[Authorize(Policy = Policies.SeesAvisosOficina)]
public class AvisosOficinaController(AvisoService service) : ControllerBase
{
    /// <summary>The ones not attended yet and the ones attended in the last 2 days, newest first.</summary>
    [HttpGet]
    public Task<AvisosOficinaDto> List(CancellationToken ct) => service.OficinaListAsync(ct);

    /// <summary>Marks it as attended for everyone.</summary>
    [HttpPost("{id:int}/atendido")]
    public Task<AvisosOficinaDto> Attend(int id, CancellationToken ct) => service.AttendAsync(id, ct);
}
