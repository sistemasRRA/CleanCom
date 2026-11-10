using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Catalogos;
using CleanCom.Dominio.Acceso;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("lineas-servicio")]
[Tags("Catálogos")]
public class LineasServicioController(ServicioCatalogos servicio) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LineaServicioDto>> Listar(CancellationToken ct) =>
        servicio.ListarLineasServicioAsync(ct);

    [HttpPost]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType<LineaServicioDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(GuardarLineaServicioRequest request, CancellationToken ct)
    {
        var linea = await servicio.GuardarLineaServicioAsync(null, request, ct);
        return Created($"/lineas-servicio/{linea.Id}", linea);
    }

    [HttpPut("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    public Task<LineaServicioDto> Actualizar(int id, GuardarLineaServicioRequest request, CancellationToken ct) =>
        servicio.GuardarLineaServicioAsync(id, request, ct);
}
