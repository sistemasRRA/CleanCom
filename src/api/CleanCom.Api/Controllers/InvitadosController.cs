using CleanCom.Api.Common;
using CleanCom.Application.Invitados;
using CleanCom.Application.Promociones;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// Invitados and their addresses, as Agendar uses them. The CRM screen will add its own endpoints and permission.
/// </summary>
[ApiController]
[Route("invitados")]
[Tags("Invitados")]
public class InvitadosController(InvitadoService service, BeneficioService beneficios, PromocionService promociones) : ControllerBase
{
    /// <summary>The invitado's sellos and cupones in every state, newest first.</summary>
    [HttpGet("{id:int}/beneficios")]
    [Authorize(Policy = Policies.SeesBeneficios)]
    public Task<IReadOnlyList<BeneficioDto>> Beneficios(int id, CancellationToken ct) => beneficios.ForInvitadoAsync(id, ct);

    /// <summary>Gives a sello or cupón by hand. Over Promociones.TopeSello it is still given, with a warning.</summary>
    [HttpPost("{id:int}/beneficios")]
    [Authorize(Policy = Policies.UsesBeneficios)]
    public Task<GrantBeneficioResultDto> Grant(int id, GrantBeneficioRequest request, CancellationToken ct) =>
        promociones.GrantAsync(id, request, ct);

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

    /// <summary>
    /// Baja lógica of the invitado with their addresses and pedidos. SuperUser and Administración only.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policies.Administrators)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/direcciones")]
    [RequireScreen(Screens.Agendar, AccessLevel.Edit)]
    public Task<InvitadoDto> AddAddress(int id, SaveAddressRequest request, CancellationToken ct) =>
        service.AddAddressAsync(id, request, ct);

    [HttpPut("{id:int}/direcciones/{addressId:int}")]
    [RequireScreen(Screens.Agendar, AccessLevel.Edit)]
    public Task<InvitadoDto> UpdateAddress(int id, int addressId, SaveAddressRequest request, CancellationToken ct) =>
        service.UpdateAddressAsync(id, addressId, request, ct);

    [HttpDelete("{id:int}/direcciones/{addressId:int}")]
    [RequireScreen(Screens.Agendar, AccessLevel.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateAddress(int id, int addressId, CancellationToken ct)
    {
        await service.DeactivateAddressAsync(id, addressId, ct);
        return NoContent();
    }
}
