using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;

namespace CleanCom.Domain.People;

/// <summary>
/// Chofer on the payroll. May or may not have a user to sign in to the app.
/// </summary>
public class Operador : Entity, IActivatable
{
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public int? UsualRutaId { get; set; }
    public Ruta? UsualRuta { get; set; }
    public bool Active { get; set; } = true;
    public int? UserId { get; set; }
}
