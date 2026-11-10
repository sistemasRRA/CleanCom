using CleanCom.Dominio.Catalogos;
using CleanCom.Dominio.Comun;

namespace CleanCom.Dominio.Personas;

/// <summary>
/// Chofer de la nómina. Puede tener o no un usuario para entrar a la app.
/// </summary>
public class Operador : Entidad, IActivable
{
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
    public int? RutaHabitualId { get; set; }
    public Ruta? RutaHabitual { get; set; }
    public bool Activo { get; set; } = true;
    public int? UsuarioId { get; set; }
}
