namespace CleanCom.Aplicacion.Acceso;

public interface IServicioAutenticacion
{
    Task<SesionDto> IniciarSesionAsync(IniciarSesionRequest request, CancellationToken ct);
    Task<SesionDto> RenovarAsync(RenovarSesionRequest request, CancellationToken ct);
    Task CerrarSesionAsync(RenovarSesionRequest request, CancellationToken ct);
    Task<SesionDto> CambiarContrasenaAsync(int usuarioId, CambiarContrasenaRequest request, CancellationToken ct);
    Task<UsuarioSesionDto> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct);
}
