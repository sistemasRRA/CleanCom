namespace CleanCom.Aplicacion.Acceso;

public record GuardarOperadorRequest(string Nombre, string? Telefono, int? RutaHabitualId, bool Activo = true);
