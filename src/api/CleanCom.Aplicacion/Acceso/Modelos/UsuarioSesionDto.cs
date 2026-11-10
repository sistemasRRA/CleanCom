namespace CleanCom.Aplicacion.Acceso;

public record UsuarioSesionDto(int Id, string NombreCompleto, string Usuario, string Rol, bool DebeCambiarContrasena, IReadOnlyList<string> Permisos);
