using CleanCom.Api.Common;
using CleanCom.Application.Agenda;
using CleanCom.Domain.Access;
using CleanCom.Domain.Pedidos;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// The single agenda engine. The invitado app will call these same endpoints with its own rules (week 5).
/// </summary>
[ApiController]
[Route("agenda")]
[Tags("Agenda")]
public class AgendaController(AgendaService service) : ControllerBase
{
    /// <summary>
    /// The agenda by day: occupancy of each ruta against its cupo, days off and scheduled pedidos.
    /// From a date (yyyy-MM-dd) for 1 to 31 days; without a date, the current week from Monday.
    /// </summary>
    [HttpGet("tablero")]
    [RequireScreen(Screens.Agendar, AccessLevel.View)]
    public Task<AgendaBoardDto> Board([FromQuery] DateOnly? from, [FromQuery] int? days, CancellationToken ct) =>
        service.BoardAsync(from, days, ct);

    /// <summary>Zona, ruta, first date with cupo, place, other days and the explanation ("Así se agenda").</summary>
    [HttpPost("disponibilidad")]
    [RequireScreen(Screens.Agendar, AccessLevel.View)]
    public Task<AvailabilityDto> Availability(AvailabilityRequest request, CancellationToken ct) =>
        service.AvailabilityAsync(request, ct);

    /// <summary>
    /// Creates the pedido (Agendado) and its recolección parada. If the day filled up meanwhile, nothing is
    /// created and the answer carries the next available date.
    /// </summary>
    [HttpPost("agendar")]
    [RequireScreen(Screens.Agendar, AccessLevel.Edit)]
    public Task<ScheduleResultDto> Schedule(ScheduleRequest request, CancellationToken ct) =>
        service.ScheduleAsync(request, PedidoOrigin.Oficina, ct);
}
