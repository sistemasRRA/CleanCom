using CleanCom.Api.Common;
using CleanCom.Application.Agenda;
using CleanCom.Application.Invitados;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// The signed-in invitado's own data and addresses (app del invitado). Each endpoint only reaches the invitado
/// linked to the session.
/// </summary>
[ApiController]
[Route("yo")]
[Tags("App del invitado")]
public class MeController(MeService service, InvitadoAgendaService agenda) : ControllerBase
{
    [HttpGet]
    [RequireScreen(Screens.AppInvitado, AccessLevel.View)]
    public Task<MeDto> Get(CancellationToken ct) => service.GetAsync(ct);

    /// <summary>Name, email, other phone, "¿cómo nos conociste?" and WhatsApp notices. The phone does not change.</summary>
    [HttpPut]
    [RequireScreen(Screens.AppInvitado, AccessLevel.Edit)]
    public Task<MeDto> Update(UpdateMeRequest request, CancellationToken ct) => service.UpdateAsync(request, ct);

    [HttpGet("direcciones")]
    [RequireScreen(Screens.AppInvitado, AccessLevel.View)]
    public async Task<IReadOnlyList<AddressDto>> Addresses(CancellationToken ct) => (await service.GetAsync(ct)).Addresses;

    /// <summary>The zona is located from the address (polygon, postal code and colonia).</summary>
    [HttpPost("direcciones")]
    [RequireScreen(Screens.AppInvitado, AccessLevel.Edit)]
    public Task<MeDto> AddAddress(SaveAddressRequest request, CancellationToken ct) => service.AddAddressAsync(request, ct);

    [HttpPut("direcciones/{addressId:int}")]
    [RequireScreen(Screens.AppInvitado, AccessLevel.Edit)]
    public Task<MeDto> UpdateAddress(int addressId, SaveAddressRequest request, CancellationToken ct) =>
        service.UpdateAddressAsync(addressId, request, ct);

    /// <summary>Baja lógica of an address.</summary>
    [HttpDelete("direcciones/{addressId:int}")]
    [RequireScreen(Screens.AppInvitado, AccessLevel.Edit)]
    public Task<MeDto> DeactivateAddress(int addressId, CancellationToken ct) => service.DeactivateAddressAsync(addressId, ct);

    /// <summary>"Mis pedidos": the invitado's pedidos, newest first. To ask for a service: POST /agenda/disponibilidad and /agenda/agendar.</summary>
    [HttpGet("pedidos")]
    [RequireScreen(Screens.AppInvitado, AccessLevel.View)]
    public Task<IReadOnlyList<MyPedidoDto>> Pedidos(CancellationToken ct) => agenda.PedidosAsync(ct);

    /// <summary>Seguimiento: the etapas with their time and today's visit. Only the invitado's own pedidos.</summary>
    [HttpGet("pedidos/{id:int}/seguimiento")]
    [RequireScreen(Screens.AppInvitado, AccessLevel.View)]
    public Task<MySeguimientoDto> Seguimiento(int id, CancellationToken ct) => agenda.SeguimientoAsync(id, ct);

    /// <summary>
    /// Days the recolección can move to: from tomorrow before Agenda.CorteCambioDia (12:00), from two days later after
    /// it; visit days of the zona with cupo, without the current one.
    /// </summary>
    [HttpGet("pedidos/{id:int}/cambiar-dia")]
    [RequireScreen(Screens.AppInvitado, AccessLevel.View)]
    public Task<AvailabilityDto> DayChangeOptions(int id, CancellationToken ct) => agenda.DayChangeOptionsAsync(id, ct);

    /// <summary>Moves the recolección; when the day filled up meanwhile, answers Scheduled false with the next one.</summary>
    [HttpPost("pedidos/{id:int}/cambiar-dia")]
    [RequireScreen(Screens.AppInvitado, AccessLevel.Edit)]
    public Task<EntregaResultDto> ChangeDay(int id, ChangeDayRequest request, CancellationToken ct) => agenda.ChangeDayAsync(id, request, ct);

    /// <summary>Cancels up to Agenda.CorteCancelacion (13:00) of the day before the recolección; afterwards only the office.</summary>
    [HttpPost("pedidos/{id:int}/cancelar")]
    [RequireScreen(Screens.AppInvitado, AccessLevel.Edit)]
    public Task<MySeguimientoDto> Cancel(int id, CancelPedidoRequest request, CancellationToken ct) => agenda.CancelAsync(id, request, ct);
}
