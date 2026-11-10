using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Acceso;
using CleanCom.Dominio.Acceso;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("operadores")]
[Tags("Usuarios")]
public class OperadoresController(ServicioOperadores servicio) : ControllerBase
{
    [HttpGet]
    [RequierePantalla(Pantallas.Usuarios, NivelAcceso.Ver)]
    public Task<IReadOnlyList<OperadorDto>> Listar([FromQuery] bool? incluirInactivos, CancellationToken ct) =>
        servicio.ListarAsync(incluirInactivos ?? false, ct);

    [HttpPost]
    [RequierePantalla(Pantallas.Usuarios, NivelAcceso.Editar)]
    [ProducesResponseType<OperadorDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(GuardarOperadorRequest request, CancellationToken ct)
    {
        var operador = await servicio.GuardarAsync(null, request, ct);
        return Created($"/operadores/{operador.Id}", operador);
    }

    [HttpPut("{id:int}")]
    [RequierePantalla(Pantallas.Usuarios, NivelAcceso.Editar)]
    public Task<OperadorDto> Actualizar(int id, GuardarOperadorRequest request, CancellationToken ct) =>
        servicio.GuardarAsync(id, request, ct);
}
