namespace CleanCom.Aplicacion.Acceso;

public record ActualizarUsuarioRequest(string NombreCompleto, string? Correo, string? Telefono, string Rol, bool Activo, int? OperadorId);
