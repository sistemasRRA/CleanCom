using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Catalogos;
using CleanCom.Dominio.Acceso;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("lugares")]
[Tags("Catálogos")]
public class LugaresController(ServicioCatalogos servicio) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LugarDto>> Listar([FromQuery] bool? incluirInactivos, CancellationToken ct) =>
        servicio.ListarLugaresAsync(incluirInactivos ?? false, ct);

    [HttpPost]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType<LugarDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(GuardarLugarRequest request, CancellationToken ct)
    {
        var lugar = await servicio.CrearLugarAsync(request, ct);
        return Created($"/lugares/{lugar.Id}", lugar);
    }

    [HttpPut("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    public Task<LugarDto> Actualizar(int id, GuardarLugarRequest request, CancellationToken ct) =>
        servicio.ActualizarLugarAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        await servicio.DesactivarLugarAsync(id, ct);
        return NoContent();
    }
}
