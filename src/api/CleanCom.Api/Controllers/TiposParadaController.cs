using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Catalogos;
using CleanCom.Dominio.Acceso;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("tipos-parada")]
[Tags("Catálogos")]
public class TiposParadaController(ServicioCatalogos servicio) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TipoParadaDto>> Listar(CancellationToken ct) =>
        servicio.ListarTiposParadaAsync(ct);

    [HttpPost]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType<TipoParadaDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(GuardarTipoParadaRequest request, CancellationToken ct)
    {
        var tipo = await servicio.GuardarTipoParadaAsync(null, request, ct);
        return Created($"/tipos-parada/{tipo.Id}", tipo);
    }

    [HttpPut("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    public Task<TipoParadaDto> Actualizar(int id, GuardarTipoParadaRequest request, CancellationToken ct) =>
        servicio.GuardarTipoParadaAsync(id, request, ct);
}
