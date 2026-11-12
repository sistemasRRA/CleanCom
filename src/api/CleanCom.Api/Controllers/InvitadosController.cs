using CleanCom.Api.Common;
using CleanCom.Application.Invitados;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// Invitados and their addresses, as Agendar uses them. The CRM screen will add its own endpoints and permission.
/// </summary>
[ApiController]
[Route("invitados")]
[Tags("Invitados")]
public class InvitadosController(InvitadoService service) : ControllerBase
{
    /// <summary>By name or phone, up to 20 results. Needs at least 2 characters.</summary>
    [HttpGet("buscar")]
    [RequireScreen(Screens.Agendar, AccessLevel.View)]
    public Task<IReadOnlyList<InvitadoDto>> Search([FromQuery] string? q, CancellationToken ct) =>
        service.SearchAsync(q, ct);

    [HttpGet("{id:int}")]
    [RequireScreen(Screens.Agendar, AccessLevel.View)]
    public Task<InvitadoDto> Get(int id, CancellationToken ct) =>
        service.GetAsync(id, ct);

    [HttpPost]
    [RequireScreen(Screens.Agendar, AccessLevel.Edit)]
    [ProducesResponseType<InvitadoDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveInvitadoRequest request, CancellationToken ct)
    {
        var invitado = await service.CreateAsync(request, ct);
        return Created($"/invitados/{invitado.Id}", invitado);
    }

    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Agendar, AccessLevel.Edit)]
    public Task<InvitadoDto> Update(int id, SaveInvitadoRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    [HttpPost("{id:int}/direcciones")]
    [RequireScreen(Screens.Agendar, AccessLevel.Edit)]
    public Task<InvitadoDto> AddDireccion(int id, SaveDireccionRequest request, CancellationToken ct) =>
        service.AddDireccionAsync(id, request, ct);

    [HttpPut("{id:int}/direcciones/{direccionId:int}")]
    [RequireScreen(Screens.Agendar, AccessLevel.Edit)]
    public Task<InvitadoDto> UpdateDireccion(int id, int direccionId, SaveDireccionRequest request, CancellationToken ct) =>
        service.UpdateDireccionAsync(id, direccionId, request, ct);

    [HttpDelete("{id:int}/direcciones/{direccionId:int}")]
    [RequireScreen(Screens.Agendar, AccessLevel.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateDireccion(int id, int direccionId, CancellationToken ct)
    {
        await service.DeactivateDireccionAsync(id, direccionId, ct);
        return NoContent();
    }
}
