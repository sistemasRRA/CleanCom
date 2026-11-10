using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Catalogos;
using CleanCom.Dominio.Acceso;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("zonas")]
[Tags("Catálogos")]
public class ZonasController(ServicioZonas servicio) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ZonaDto>> Listar([FromQuery] int? rutaId, [FromQuery] bool? incluirInactivas, CancellationToken ct) =>
        servicio.ListarAsync(rutaId, incluirInactivas ?? false, ct);

    [HttpGet("{id:int}")]
    public Task<ZonaDto> Obtener(int id, CancellationToken ct) =>
        servicio.ObtenerAsync(id, ct);

    [HttpPost]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType<ZonaDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(GuardarZonaRequest request, CancellationToken ct)
    {
        var zona = await servicio.CrearAsync(request, ct);
        return Created($"/zonas/{zona.Id}", zona);
    }

    [HttpPut("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    public Task<ZonaDto> Actualizar(int id, GuardarZonaRequest request, CancellationToken ct) =>
        servicio.ActualizarAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        await servicio.DesactivarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Días de visita de una zona núcleo; las zonas EXT los heredan.</summary>
    [HttpPut("{id:int}/dias")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    public Task<ZonaDto> CambiarDias(int id, DiasVisitaRequest request, CancellationToken ct) =>
        servicio.CambiarDiasAsync(id, request, ct);
}
