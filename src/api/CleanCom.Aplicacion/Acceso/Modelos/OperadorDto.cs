namespace CleanCom.Aplicacion.Acceso;

public record OperadorDto(int Id, string Nombre, string? Telefono, int? RutaHabitualId, bool Activo, int? UsuarioId);
