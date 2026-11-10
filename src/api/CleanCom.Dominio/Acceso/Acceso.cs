using CleanCom.Dominio.Comun;

namespace CleanCom.Dominio.Acceso;

public static class Roles
{
    public const string Administracion = "Administracion";
    public const string Chofer = "Chofer";
    public const string Invitado = "Invitado";

    public const int IdAdministracion = 1;
    public const int IdChofer = 2;
    public const int IdInvitado = 3;
}

public enum NivelAcceso
{
    Ver = 1,
    Editar = 2,
}

public record Pantalla(string Clave, string Nombre);

/// <summary>
/// Pantallas del sistema. Los permisos se asignan por pantalla para poder agregar roles sin desarrollo.
/// </summary>
public static class Pantallas
{
    public const string PanelDelDia = "panel-del-dia";
    public const string Rutas = "rutas";
    public const string Agendar = "agendar";
    public const string Pedidos = "pedidos";
    public const string ClientesCrm = "clientes-crm";
    public const string ParadasFijas = "paradas-fijas";
    public const string Promociones = "promociones";
    public const string TicketsEnPlanta = "tickets-en-planta";
    public const string Kpis = "kpis";
    public const string ClientesYFuentes = "clientes-y-fuentes";
    public const string EvidenciasYCaja = "evidencias-y-caja";
    public const string VistaDelChofer = "vista-del-chofer";
    public const string Usuarios = "usuarios";
    public const string Operacion = "operacion";
    public const string Unidades = "unidades";
    public const string WhatsAppYMetas = "whatsapp-y-metas";
    public const string AppChofer = "app-chofer";
    public const string AppInvitado = "app-invitado";

    public static IReadOnlyList<Pantalla> Todas { get; } =
    [
        new(PanelDelDia, "Panel del día"),
        new(Rutas, "Rutas"),
        new(Agendar, "Agendar"),
        new(Pedidos, "Pedidos"),
        new(ClientesCrm, "Clientes (CRM)"),
        new(ParadasFijas, "Paradas fijas"),
        new(Promociones, "Promociones"),
        new(TicketsEnPlanta, "Tickets en planta"),
        new(Kpis, "KPIs"),
        new(ClientesYFuentes, "Clientes y fuentes"),
        new(EvidenciasYCaja, "Evidencias y caja"),
        new(VistaDelChofer, "Vista del chofer"),
        new(Usuarios, "Usuarios"),
        new(Operacion, "Operación"),
        new(Unidades, "Unidades"),
        new(WhatsAppYMetas, "WhatsApp y metas"),
        new(AppChofer, "App del chofer"),
        new(AppInvitado, "App del invitado"),
    ];

    public static bool Existe(string clave) => Todas.Any(p => p.Clave == clave);
}

/// <summary>
/// Nivel de acceso de un rol a una pantalla.
/// </summary>
public class PermisoRol : Entidad
{
    public int RolId { get; set; }
    public string Pantalla { get; set; } = "";
    public NivelAcceso Nivel { get; set; }
}

/// <summary>
/// Permisos en el token: un claim "permiso" por pantalla con el formato "pantalla:nivel".
/// </summary>
public static class Permisos
{
    public const string TipoClaim = "permiso";

    public static string AClaim(string pantalla, NivelAcceso nivel) => $"{pantalla}:{nivel.ToString().ToLowerInvariant()}";

    /// <summary>
    /// Indica si los permisos del usuario alcanzan el nivel pedido. Editar incluye Ver.
    /// </summary>
    public static bool Permite(IEnumerable<string> permisos, string pantalla, NivelAcceso requerido)
    {
        foreach (var permiso in permisos)
        {
            var partes = permiso.Split(':');
            if (partes.Length == 2
                && partes[0] == pantalla
                && Enum.TryParse<NivelAcceso>(partes[1], ignoreCase: true, out var nivel)
                && nivel >= requerido)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Permisos iniciales: Administración edita todo lo de oficina; cada app es de su rol.
    /// </summary>
    public static IReadOnlyList<(int RolId, string Pantalla, NivelAcceso Nivel)> Iniciales { get; } =
    [
        .. Pantallas.Todas
            .Where(p => p.Clave is not Pantallas.AppChofer and not Pantallas.AppInvitado)
            .Select(p => (Roles.IdAdministracion, p.Clave, NivelAcceso.Editar)),
        (Roles.IdChofer, Pantallas.AppChofer, NivelAcceso.Editar),
        (Roles.IdInvitado, Pantallas.AppInvitado, NivelAcceso.Editar),
    ];
}
