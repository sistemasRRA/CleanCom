using CleanCom.Api.Common;
using CleanCom.Application.Catalogs;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("lugares")]
[Tags("Catálogos")]
public class LugaresController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LugarDto>> List([FromQuery] bool? includeInactive, CancellationToken ct) =>
        service.ListLugaresAsync(includeInactive ?? false, ct);

    [HttpPost]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType<LugarDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveLugarRequest request, CancellationToken ct)
    {
        var lugar = await service.CreateLugarAsync(request, ct);
        return Created($"/lugares/{lugar.Id}", lugar);
    }

    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    public Task<LugarDto> Update(int id, SaveLugarRequest request, CancellationToken ct) =>
        service.UpdateLugarAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await service.DeactivateLugarAsync(id, ct);
        return NoContent();
    }
}
