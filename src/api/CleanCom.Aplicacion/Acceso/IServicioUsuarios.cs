namespace CleanCom.Aplicacion.Acceso;

public interface IServicioUsuarios
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(string? rol, CancellationToken ct);
    Task<UsuarioDto> CrearAsync(CrearUsuarioRequest request, CancellationToken ct);
    Task<UsuarioDto> ActualizarAsync(int id, ActualizarUsuarioRequest request, CancellationToken ct);
    Task RestablecerContrasenaAsync(int id, RestablecerContrasenaRequest request, CancellationToken ct);
}
