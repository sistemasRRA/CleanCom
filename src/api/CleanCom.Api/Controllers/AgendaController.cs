using CleanCom.Api.Common;
using CleanCom.Application.Agenda;
using CleanCom.Application.Invitados;
using Microsoft.AspNetCore.Authorization;
using CleanCom.Domain.Access;
using CleanCom.Domain.Pedidos;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// The single agenda engine. The invitado app calls these same endpoints and gets the Invitado role's rules.
/// </summary>
[ApiController]
[Route("agenda")]
[Tags("Agenda")]
public class AgendaController(AgendaService service, InvitadoAgendaService invitado) : ControllerBase
{
    /// <summary>
    /// The agenda by day: occupancy of each ruta against its cupo, days off and scheduled pedidos.
    /// From a date (yyyy-MM-dd) for 1 to 31 days; without a date, the current week from Monday.
    /// </summary>
    [HttpGet("tablero")]
    [RequireScreen(Screens.Agendar, AccessLevel.View)]
    public Task<AgendaBoardDto> Board([FromQuery] DateOnly? from, [FromQuery] int? days, CancellationToken ct) =>
        service.BoardAsync(from, days, ct);

    /// <summary>
    /// Zona, ruta, first date with cupo, place, other days and the explanation ("Así se agenda"). From the invitado app
    /// only its own addresses, without zona chosen by hand nor hora de registro.
    /// </summary>
    [HttpPost("disponibilidad")]
    [Authorize(Policy = Policies.UsesAgenda)]
    public Task<AvailabilityDto> Availability(AvailabilityRequest request, CancellationToken ct) =>
        User.IsInRole(Roles.Invitado) ? invitado.AvailabilityAsync(request, ct) : service.AvailabilityAsync(request, ct);

    /// <summary>
    /// Creates the pedido (Agendado) and its recolección parada. If the day filled up meanwhile, nothing is
    /// created and the answer carries the next available date. From the invitado app: only for itself and its
    /// addresses, without specific time, priority nor hora de registro; the pedido's origin is App.
    /// </summary>
    [HttpPost("agendar")]
    [Authorize(Policy = Policies.SchedulesAgenda)]
    public Task<ScheduleResultDto> Schedule(ScheduleRequest request, CancellationToken ct) =>
        User.IsInRole(Roles.Invitado) ? invitado.ScheduleAsync(request, ct) : service.ScheduleAsync(request, PedidoOrigin.Oficina, ct);
}
