using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Catalogos;
using CleanCom.Dominio.Acceso;
using CleanCom.Dominio.Catalogos;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("motivos")]
[Tags("Catálogos")]
public class MotivosController(ServicioCatalogos servicio) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<MotivoDto>> Listar([FromQuery] TipoMotivo? tipo, CancellationToken ct) =>
        servicio.ListarMotivosAsync(tipo, ct);

    [HttpPost]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    [ProducesResponseType<MotivoDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(GuardarMotivoRequest request, CancellationToken ct)
    {
        var motivo = await servicio.GuardarMotivoAsync(null, request, ct);
        return Created($"/motivos/{motivo.Id}", motivo);
    }

    [HttpPut("{id:int}")]
    [RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]
    public Task<MotivoDto> Actualizar(int id, GuardarMotivoRequest request, CancellationToken ct) =>
        servicio.GuardarMotivoAsync(id, request, ct);
}
