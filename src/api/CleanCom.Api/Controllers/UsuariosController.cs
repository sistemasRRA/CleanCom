using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Acceso;
using CleanCom.Dominio.Acceso;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("usuarios")]
[Tags("Usuarios")]
public class UsuariosController(IServicioUsuarios servicio) : ControllerBase
{
    [HttpGet]
    [RequierePantalla(Pantallas.Usuarios, NivelAcceso.Ver)]
    public Task<IReadOnlyList<UsuarioDto>> Listar([FromQuery] string? rol, CancellationToken ct) =>
        servicio.ListarAsync(rol, ct);

    [HttpPost]
    [RequierePantalla(Pantallas.Usuarios, NivelAcceso.Editar)]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(CrearUsuarioRequest request, CancellationToken ct)
    {
        var usuario = await servicio.CrearAsync(request, ct);
        return Created($"/usuarios/{usuario.Id}", usuario);
    }

    [HttpPut("{id:int}")]
    [RequierePantalla(Pantallas.Usuarios, NivelAcceso.Editar)]
    public Task<UsuarioDto> Actualizar(int id, ActualizarUsuarioRequest request, CancellationToken ct) =>
        servicio.ActualizarAsync(id, request, ct);

    /// <summary>Asigna una contraseña temporal; el usuario debe cambiarla al entrar.</summary>
    [HttpPost("{id:int}/restablecer")]
    [RequierePantalla(Pantallas.Usuarios, NivelAcceso.Editar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RestablecerContrasena(int id, RestablecerContrasenaRequest request, CancellationToken ct)
    {
        await servicio.RestablecerContrasenaAsync(id, request, ct);
        return NoContent();
    }
}
