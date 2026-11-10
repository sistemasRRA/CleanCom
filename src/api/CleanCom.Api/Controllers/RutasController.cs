using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Catalogos;
using CleanCom.Dominio.Acceso;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Catálogos de Operación: leer pide sesión; cambiar pide editar Operación.</summary>
[ApiController]
[Route("rutas")]
[Tags("Catálogos")]
public class RutasController(ServicioCatalogos servicio) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RutaDto>> Listar(CancellationToken ct) =>
        servicio.ListarRutasAsync(ct);

    [HttpPost]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType<RutaDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(GuardarRutaRequest request, CancellationToken ct)
    {
        var ruta = await servicio.CrearRutaAsync(request, ct);
        return Created($"/rutas/{ruta.Id}", ruta);
    }

    [HttpPut("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    public Task<RutaDto> Actualizar(int id, GuardarRutaRequest request, CancellationToken ct) =>
        servicio.ActualizarRutaAsync(id, request, ct);
}
