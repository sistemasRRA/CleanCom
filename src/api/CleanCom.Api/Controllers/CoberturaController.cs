using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Catalogos;
using CleanCom.Dominio.Acceso;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Códigos postales y colonias con servicio, ligados a su zona.</summary>
[ApiController]
[Route("cobertura")]
[Tags("Catálogos")]
public class CoberturaController(ServicioCatalogos servicio) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CoberturaDto>> Listar([FromQuery] string? buscar, [FromQuery] int? zonaId, CancellationToken ct) =>
        servicio.ListarCoberturaAsync(buscar, zonaId, ct);

    [HttpPost]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType<CoberturaDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(GuardarCoberturaRequest request, CancellationToken ct)
    {
        var cobertura = await servicio.GuardarCoberturaAsync(null, request, ct);
        return Created($"/cobertura/{cobertura.Id}", cobertura);
    }

    [HttpPut("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    public Task<CoberturaDto> Actualizar(int id, GuardarCoberturaRequest request, CancellationToken ct) =>
        servicio.GuardarCoberturaAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await servicio.EliminarCoberturaAsync(id, ct);
        return NoContent();
    }
}
