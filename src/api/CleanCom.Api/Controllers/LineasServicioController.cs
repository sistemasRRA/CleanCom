using CleanCom.Api.Common;
using CleanCom.Application.Catalogs;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("lineas-servicio")]
[Tags("Catálogos")]
public class LineasServicioController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LineaServicioDto>> List(CancellationToken ct) =>
        service.ListLineasServicioAsync(ct);

    [HttpPost]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType<LineaServicioDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveLineaServicioRequest request, CancellationToken ct)
    {
        var linea = await service.SaveLineaServicioAsync(null, request, ct);
        return Created($"/lineas-servicio/{linea.Id}", linea);
    }

    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    public Task<LineaServicioDto> Update(int id, SaveLineaServicioRequest request, CancellationToken ct) =>
        service.SaveLineaServicioAsync(id, request, ct);
}
