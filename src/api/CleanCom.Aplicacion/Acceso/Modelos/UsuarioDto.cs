namespace CleanCom.Aplicacion.Acceso;

public record UsuarioDto(
    int Id,
    string NombreCompleto,
    string Usuario,
    string? Correo,
    string? Telefono,
    string Rol,
    bool Activo,
    bool DebeCambiarContrasena,
    DateTimeOffset? UltimoAcceso,
    int? OperadorId,
    string? OperadorNombre);
