using CleanCom.Api.Common;
using CleanCom.Application.Catalogs;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("zonas")]
[Tags("Catálogos")]
public class ZonasController(ZonaService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ZonaDto>> List([FromQuery] int? rutaId, [FromQuery] bool? includeInactive, CancellationToken ct) =>
        service.ListAsync(rutaId, includeInactive ?? false, ct);

    [HttpGet("{id:int}")]
    public Task<ZonaDto> Get(int id, CancellationToken ct) =>
        service.GetAsync(id, ct);

    [HttpPost]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType<ZonaDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveZonaRequest request, CancellationToken ct)
    {
        var zona = await service.CreateAsync(request, ct);
        return Created($"/zonas/{zona.Id}", zona);
    }

    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    public Task<ZonaDto> Update(int id, SaveZonaRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await service.DeactivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>Visit days of a zona núcleo; EXT zonas inherit them.</summary>
    [HttpPut("{id:int}/dias")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    public Task<ZonaDto> ChangeVisitDays(int id, VisitDaysRequest request, CancellationToken ct) =>
        service.ChangeVisitDaysAsync(id, request, ct);
}
