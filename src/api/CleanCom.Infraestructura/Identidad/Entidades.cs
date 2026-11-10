using Microsoft.AspNetCore.Identity;

namespace CleanCom.Infraestructura.Identidad;

/// <summary>
/// Usuario de ASP.NET Identity. Roles: Administracion, Chofer, Invitado.
/// </summary>
public class Usuario : IdentityUser<int>
{
    public string NombreCompleto { get; set; } = "";
    public bool Activo { get; set; } = true;
    /// <summary>La contraseña inicial o temporal se cambia en el siguiente acceso.</summary>
    public bool DebeCambiarContrasena { get; set; } = true;
    public DateTimeOffset? UltimoAcceso { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
}

public class Rol : IdentityRole<int>
{
    public Rol() { }

    public Rol(string nombre) : base(nombre) { }
}

/// <summary>
/// Token de renovación. Solo se guarda su hash; cada uso lo reemplaza por uno nuevo.
/// </summary>
public class TokenRenovacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ExpiraEn { get; set; }
    public DateTimeOffset? RevocadoEn { get; set; }
}
