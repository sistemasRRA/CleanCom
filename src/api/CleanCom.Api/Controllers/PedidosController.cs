using CleanCom.Api.Common;
using CleanCom.Application.Agenda;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("pedidos")]
[Tags("Pedidos")]
public class PedidosController(PedidoService service, PedidoBoardService board, AgendaService agenda) : ControllerBase
{
    /// <summary>The board by etapa. Filters: ruta, text (invitado, folio or phone) and only late or with failed attempts.</summary>
    [HttpGet("tablero")]
    [RequireScreen(Screens.Pedidos, AccessLevel.View)]
    public Task<PedidoBoardDto> Board([FromQuery(Name = "ruta")] int? rutaId, [FromQuery] string? q, [FromQuery(Name = "atrasados")] bool onlyLate, CancellationToken ct) =>
        board.BoardAsync(rutaId, q, onlyLate, ct);

    /// <summary>The card with its history and visits.</summary>
    [HttpGet("{id:int}")]
    [RequireScreen(Screens.Pedidos, AccessLevel.View)]
    public Task<PedidoDetailDto> Get(int id, CancellationToken ct) =>
        board.GetAsync(id, ct);

    /// <summary>Move by hand; going back, delivering or cancelling needs a motivo.</summary>
    [HttpPost("{id:int}/etapa")]
    [RequireScreen(Screens.Pedidos, AccessLevel.Edit)]
    public Task<PedidoDetailDto> Move(int id, MoveEtapaRequest request, CancellationToken ct) =>
        board.MoveAsync(id, request, ct);

    [HttpPost("{id:int}/comentarios")]
    [RequireScreen(Screens.Pedidos, AccessLevel.Edit)]
    public Task<PedidoDetailDto> Comment(int id, PedidoCommentRequest request, CancellationToken ct) =>
        board.CommentAsync(id, request, ct);

    /// <summary>Days with cupo for the entrega (same engine as the agenda).</summary>
    [HttpGet("{id:int}/fechas-entrega")]
    [RequireScreen(Screens.Pedidos, AccessLevel.View)]
    public Task<AvailabilityDto> EntregaDays(int id, CancellationToken ct) =>
        agenda.EntregaAvailabilityAsync(id, ct);

    /// <summary>Programar entrega or change its date; without a date, the first day with cupo.</summary>
    [HttpPost("{id:int}/programar-entrega")]
    [RequireScreen(Screens.Pedidos, AccessLevel.Edit)]
    public Task<EntregaResultDto> ScheduleEntrega(int id, ScheduleEntregaRequest request, CancellationToken ct) =>
        agenda.ScheduleEntregaAsync(id, request, ct);

    /// <summary>No se entregó: motivo, attempt in the history and rescheduled to the next day with cupo.</summary>
    [HttpPost("{id:int}/no-entregado")]
    [RequireScreen(Screens.Pedidos, AccessLevel.Edit)]
    public Task<EntregaResultDto> NoEntregado(int id, NoEntregadoRequest request, CancellationToken ct) =>
        agenda.NoEntregadoAsync(id, request, ct);

    /// <summary>
    /// Pedidos by recolección date (yyyy-MM-dd). Without filters, from today on; with an invitado, all of theirs.
    /// </summary>
    [HttpGet]
    [RequireScreen(Screens.Agendar, AccessLevel.View)]
    public Task<IReadOnlyList<PedidoDto>> List([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? invitadoId, CancellationToken ct) =>
        service.ListAsync(from, to, invitadoId, ct);

    /// <summary>Baja lógica of the pedido and its paradas (frees its cupo). SuperUser and Administración only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policies.Administrators)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeactivateAsync(id, ct);
        return NoContent();
    }
}
