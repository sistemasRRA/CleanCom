using CleanCom.Api.Common;
using CleanCom.Application.Catalogs;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Operación catalogs: reading requires a session; changing requires editing Operación.</summary>
[ApiController]
[Route("rutas")]
[Tags("Catálogos")]
public class RutasController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RutaDto>> List(CancellationToken ct) =>
        service.ListRutasAsync(ct);

    [HttpPost]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType<RutaDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveRutaRequest request, CancellationToken ct)
    {
        var ruta = await service.CreateRutaAsync(request, ct);
        return Created($"/rutas/{ruta.Id}", ruta);
    }

    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    public Task<RutaDto> Update(int id, SaveRutaRequest request, CancellationToken ct) =>
        service.UpdateRutaAsync(id, request, ct);
}
