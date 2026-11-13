using CleanCom.Api.Common;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("pedidos")]
[Tags("Pedidos")]
public class PedidosController(PedidoService service) : ControllerBase
{
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
