namespace CleanCom.Aplicacion.Acceso;

/// <param name="OperadorId">Operador existente que se liga al usuario (solo Chofer).</param>
/// <param name="CrearOperador">Crea un operador nuevo con el nombre y teléfono del usuario (solo Chofer).</param>
public record CrearUsuarioRequest(
    string NombreCompleto,
    string Usuario,
    string? Correo,
    string? Telefono,
    string Rol,
    string ContrasenaInicial,
    int? OperadorId,
    bool CrearOperador,
    int? RutaHabitualId);
