using CleanCom.Api.Common;
using CleanCom.Application.Catalogs;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("tipos-parada")]
[Tags("Catálogos")]
public class ParadaTypesController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ParadaTypeDto>> List(CancellationToken ct) =>
        service.ListParadaTypesAsync(ct);

    [HttpPost]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType<ParadaTypeDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveParadaTypeRequest request, CancellationToken ct)
    {
        var type = await service.SaveParadaTypeAsync(null, request, ct);
        return Created($"/tipos-parada/{type.Id}", type);
    }

    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    public Task<ParadaTypeDto> Update(int id, SaveParadaTypeRequest request, CancellationToken ct) =>
        service.SaveParadaTypeAsync(id, request, ct);
}
