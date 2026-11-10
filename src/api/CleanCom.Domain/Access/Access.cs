using CleanCom.Domain.Common;

namespace CleanCom.Domain.Access;

/// <summary>
/// Role names are data (stored in the Roles table and sent in the token), so they keep their Spanish names.
/// </summary>
public static class Roles
{
    public const string Administracion = "Administracion";
    public const string Chofer = "Chofer";
    public const string Invitado = "Invitado";

    public const int AdministracionId = 1;
    public const int ChoferId = 2;
    public const int InvitadoId = 3;
}

public enum AccessLevel
{
    View = 1,
    Edit = 2,
}

public record Screen(string Key, string Name);

/// <summary>
/// Screens of the system. Permissions are granted per screen so new roles can be added without code changes.
/// Keys and names are data shown in the UI and stored in RolePermissions, so they stay in Spanish.
/// </summary>
public static class Screens
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

    public static IReadOnlyList<Screen> All { get; } =
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

    public static bool Exists(string key) => All.Any(s => s.Key == key);
}

/// <summary>
/// Access level of a role on a screen.
/// </summary>
public class RolePermission : Entity
{
    public int RoleId { get; set; }
    public string Screen { get; set; } = "";
    public AccessLevel Level { get; set; }
}

/// <summary>
/// Permissions in the token: one "permission" claim per screen, formatted as "screen:level".
/// </summary>
public static class Permissions
{
    public const string ClaimType = "permission";

    public static string ToClaim(string screen, AccessLevel level) => $"{screen}:{level.ToString().ToLowerInvariant()}";

    /// <summary>
    /// Whether the user's permissions reach the requested level. Edit includes View.
    /// </summary>
    public static bool Allows(IEnumerable<string> permissions, string screen, AccessLevel required)
    {
        foreach (var permission in permissions)
        {
            var parts = permission.Split(':');
            if (parts.Length == 2
                && parts[0] == screen
                && Enum.TryParse<AccessLevel>(parts[1], ignoreCase: true, out var level)
                && level >= required)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Initial permissions: Administracion edits every office screen; each app belongs to its role.
    /// </summary>
    public static IReadOnlyList<(int RoleId, string Screen, AccessLevel Level)> Initial { get; } =
    [
        .. Screens.All
            .Where(s => s.Key is not Screens.AppChofer and not Screens.AppInvitado)
            .Select(s => (Roles.AdministracionId, s.Key, AccessLevel.Edit)),
        (Roles.ChoferId, Screens.AppChofer, AccessLevel.Edit),
        (Roles.InvitadoId, Screens.AppInvitado, AccessLevel.Edit),
    ];
}
