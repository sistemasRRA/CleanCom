using CleanCom.Api.Common;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("planta")]
[Tags("Planta")]
public class PlantaController(PlantaService service) : ControllerBase
{
    /// <summary>Pedidos with tickets, by urgency, with the indicators. Vista: EnProceso (default) or Listos.</summary>
    [HttpGet("tickets")]
    [RequireScreen(Screens.TicketsEnPlanta, AccessLevel.View)]
    public Task<PlantaBoardDto> Board([FromQuery(Name = "vista")] PlantaView view, CancellationToken ct) =>
        service.BoardAsync(view, ct);

    /// <summary>Captures a ticket by hand while the POS connection is pending; the first one moves the pedido to En planta.</summary>
    [HttpPost("tickets")]
    [RequireScreen(Screens.TicketsEnPlanta, AccessLevel.Edit)]
    public Task<PlantaCardDto> Capture(CaptureTicketRequest request, CancellationToken ct) =>
        service.CaptureAsync(request, ct);

    /// <summary>Scanned or typed folio: the ticket is ready; the last one of the pedido moves it to Listo.</summary>
    [HttpPost("tickets/{folio}/listo")]
    [RequireScreen(Screens.TicketsEnPlanta, AccessLevel.Edit)]
    public Task<TicketReadyResultDto> Ready(string folio, CancellationToken ct) =>
        service.ReadyAsync(folio, ct);

    /// <summary>Baja lógica of a ticket captured by mistake (only while in process).</summary>
    [HttpPost("tickets/{id:int}/baja")]
    [RequireScreen(Screens.TicketsEnPlanta, AccessLevel.Edit)]
    public Task<PlantaCardDto> Deactivate(int id, DeactivateTicketRequest request, CancellationToken ct) =>
        service.DeactivateAsync(id, request, ct);

    /// <summary>"¿Por qué se atrasa?": motivo and/or comment, visible in Pedidos.</summary>
    [HttpPost("comentarios")]
    [RequireScreen(Screens.TicketsEnPlanta, AccessLevel.Edit)]
    public Task<PlantaCardDto> Comment(PlantaCommentRequest request, CancellationToken ct) =>
        service.CommentAsync(request, ct);
}
