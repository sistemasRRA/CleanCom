using CleanCom.Api.Infraestructura;
using CleanCom.Aplicacion.Acceso;
using CleanCom.Aplicacion.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Inicio y cierre de sesión, renovación del token y datos del usuario actual.</summary>
[ApiController]
[Route("auth")]
[Tags("Sesión")]
public class SesionController(IServicioAutenticacion servicio) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<SesionDto> IniciarSesion(IniciarSesionRequest request, CancellationToken ct) =>
        servicio.IniciarSesionAsync(request, ct);

    [HttpPost("renovar")]
    [AllowAnonymous]
    public Task<SesionDto> Renovar(RenovarSesionRequest request, CancellationToken ct) =>
        servicio.RenovarAsync(request, ct);

    [HttpPost("cerrar")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cerrar(RenovarSesionRequest request, CancellationToken ct)
    {
        await servicio.CerrarSesionAsync(request, ct);
        return NoContent();
    }

    /// <summary>Disponible aunque falte cambiar la contraseña inicial.</summary>
    [HttpPost("cambiar-contrasena")]
    [Authorize(Politicas.SesionIniciada)]
    public Task<SesionDto> CambiarContrasena(CambiarContrasenaRequest request, CancellationToken ct) =>
        servicio.CambiarContrasenaAsync(IdUsuario(), request, ct);

    /// <summary>Disponible aunque falte cambiar la contraseña inicial.</summary>
    [HttpGet("yo")]
    [Authorize(Politicas.SesionIniciada)]
    public Task<UsuarioSesionDto> Yo(CancellationToken ct) =>
        servicio.ObtenerUsuarioAsync(IdUsuario(), ct);

    private int IdUsuario() =>
        User.IdUsuario() ?? throw new ExcepcionNoAutorizado("La sesión no es válida.");
}
