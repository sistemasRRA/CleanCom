using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Catalogos;
using CleanCom.Dominio.Acceso;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("dias-inhabiles")]
[Tags("Catálogos")]
public class DiasInhabilesController(ServicioCatalogos servicio) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<DiaInhabilDto>> Listar([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) =>
        servicio.ListarDiasInhabilesAsync(desde, hasta, ct);

    [HttpPost]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType<DiaInhabilDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(GuardarDiaInhabilRequest request, CancellationToken ct)
    {
        var dia = await servicio.GuardarDiaInhabilAsync(null, request, ct);
        return Created($"/dias-inhabiles/{dia.Id}", dia);
    }

    [HttpPut("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    public Task<DiaInhabilDto> Actualizar(int id, GuardarDiaInhabilRequest request, CancellationToken ct) =>
        servicio.GuardarDiaInhabilAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await servicio.EliminarDiaInhabilAsync(id, ct);
        return NoContent();
    }
}
