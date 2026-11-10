using System.Globalization;
using System.Text;
using CleanCom.Domain.Common;

namespace CleanCom.Domain.Access;

/// <summary>
/// Role keys are data (stored in the Roles table and sent in the token), so they keep their Spanish names.
/// The office can add more roles from Usuarios › Roles y permisos; these are the ones the system ships with.
/// </summary>
public static class Roles
{
    public const string Administracion = "Administracion";
    public const string Chofer = "Chofer";
    public const string Invitado = "Invitado";
    public const string Gerencia = "Gerencia";
    public const string Logistica = "Logistica";
    public const string Telemarketing = "Telemarketing";
    public const string Planta = "Planta";
    public const string SuperUser = "SuperUser";

    public const int AdministracionId = 1;
    public const int ChoferId = 2;
    public const int InvitadoId = 3;
    public const int GerenciaId = 4;
    public const int LogisticaId = 5;
    public const int TelemarketingId = 6;
    public const int PlantaId = 7;
    public const int SuperUserId = 8;

    /// <summary>Roles created with the system: id, key and the name shown to the user.</summary>
    public static IReadOnlyList<(int Id, string Key, string Name)> System { get; } =
    [
        (AdministracionId, Administracion, "Administración"),
        (ChoferId, Chofer, "Chofer"),
        (InvitadoId, Invitado, "Invitado"),
        (GerenciaId, Gerencia, "Gerencia"),
        (LogisticaId, Logistica, "Logística"),
        (TelemarketingId, Telemarketing, "Telemarketing"),
        (PlantaId, Planta, "Planta"),
        (SuperUserId, SuperUser, "SuperUser"),
    ];

    public static bool IsSystem(string key) => System.Any(r => r.Key == key);

    /// <summary>SuperUser always has every office screen so the system can never be left without access.</summary>
    public static bool HasLockedPermissions(string key) => key == SuperUser;

    /// <summary>Invitados sign up from their app; the office never assigns this role.</summary>
    public static bool IsAssignableInOffice(string key) => key != Invitado;

    /// <summary>
    /// Key for a role created by the office: no accents, spaces or symbols, each word capitalized.
    /// "Atención a clientes" → "AtencionAClientes".
    /// </summary>
    public static string KeyFromName(string name)
    {
        var plain = new StringBuilder();
        foreach (var c in name.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                plain.Append(c);
            }
        }

        var key = new StringBuilder();
        foreach (var word in plain.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var letters = new string(word.Where(char.IsAsciiLetterOrDigit).ToArray());
            if (letters.Length > 0)
            {
                key.Append(char.ToUpperInvariant(letters[0])).Append(letters[1..].ToLowerInvariant());
            }
        }
        return key.ToString();
    }
}

public enum AccessLevel
{
    View = 1,
    Edit = 2,
}

/// <param name="Group">Section of the side menu (or "Apps") where the screen appears.</param>
public record Screen(string Key, string Name, string Group);

/// <summary>
/// Screens of the system. Permissions are granted per screen so new roles can be added without code changes.
/// Keys, names and groups are data shown in the UI and stored in RolePermissions, so they stay in Spanish.
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
    public const string RolesYPermisos = "roles-y-permisos";

    private const string OperacionDiaria = "Operación diaria";
    private const string Clientes = "Clientes";
    private const string GerenciaGroup = "Gerencia";
    private const string AdministracionGroup = "Administración";
    private const string Apps = "Apps";

    /// <summary>
    /// The order fixes the ids of the seeded permissions, so new screens go at the end.
    /// The web orders them as its side menu does.
    /// </summary>
    public static IReadOnlyList<Screen> All { get; } =
    [
        new(PanelDelDia, "Panel del día", OperacionDiaria),
        new(Rutas, "Rutas", OperacionDiaria),
        new(Agendar, "Agendar", OperacionDiaria),
        new(Pedidos, "Pedidos", OperacionDiaria),
        new(ClientesCrm, "Clientes (CRM)", Clientes),
        new(ParadasFijas, "Paradas fijas", OperacionDiaria),
        new(Promociones, "Promociones", Clientes),
        new(TicketsEnPlanta, "Tickets en planta", OperacionDiaria),
        new(Kpis, "KPIs", GerenciaGroup),
        new(ClientesYFuentes, "Clientes y fuentes", GerenciaGroup),
        new(EvidenciasYCaja, "Evidencias y caja", GerenciaGroup),
        new(VistaDelChofer, "Vista del chofer", OperacionDiaria),
        new(Usuarios, "Usuarios", AdministracionGroup),
        new(Operacion, "Operación", AdministracionGroup),
        new(Unidades, "Unidades", AdministracionGroup),
        new(WhatsAppYMetas, "WhatsApp y metas", Clientes),
        new(AppChofer, "App del chofer", Apps),
        new(AppInvitado, "App del invitado", Apps),
        new(RolesYPermisos, "Roles y permisos", AdministracionGroup),
    ];

    public static IEnumerable<Screen> Office => All.Where(s => s.Group != Apps);

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
/// Permissions as "screen:level" strings, the format the clients receive in the session.
/// </summary>
public static class Permissions
{
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

    /// <summary>What SuperUser always has: edit on every office screen.</summary>
    public static IEnumerable<(string Screen, AccessLevel Level)> FullOffice =>
        Screens.Office.Select(s => (s.Key, AccessLevel.Edit));

    /// <summary>
    /// Initial permissions. SuperUser needs none (it always has everything). Administracion edits every
    /// office screen except Roles y permisos; each app belongs to its role.
    /// Gerencia, Logistica, Telemarketing and Planta start with a proposal the office adjusts in
    /// Usuarios › Roles y permisos (see docs/09-pendientes.md). New entries go at the end so seed ids do not move.
    /// </summary>
    public static IReadOnlyList<(int RoleId, string Screen, AccessLevel Level)> Initial { get; } =
    [
        .. Screens.Office
            .Where(s => s.Key != Screens.RolesYPermisos)
            .Select(s => (Roles.AdministracionId, s.Key, AccessLevel.Edit)),
        (Roles.ChoferId, Screens.AppChofer, AccessLevel.Edit),
        (Roles.InvitadoId, Screens.AppInvitado, AccessLevel.Edit),

        .. Screens.Office
            .Where(s => s.Key is not Screens.Usuarios and not Screens.Operacion and not Screens.RolesYPermisos)
            .Select(s => (Roles.GerenciaId, s.Key, AccessLevel.View)),

        (Roles.LogisticaId, Screens.PanelDelDia, AccessLevel.Edit),
        (Roles.LogisticaId, Screens.Rutas, AccessLevel.Edit),
        (Roles.LogisticaId, Screens.ParadasFijas, AccessLevel.Edit),
        (Roles.LogisticaId, Screens.Pedidos, AccessLevel.Edit),
        (Roles.LogisticaId, Screens.VistaDelChofer, AccessLevel.View),
        (Roles.LogisticaId, Screens.Unidades, AccessLevel.Edit),
        (Roles.LogisticaId, Screens.Agendar, AccessLevel.View),

        (Roles.TelemarketingId, Screens.Agendar, AccessLevel.Edit),
        (Roles.TelemarketingId, Screens.ClientesCrm, AccessLevel.Edit),
        (Roles.TelemarketingId, Screens.WhatsAppYMetas, AccessLevel.Edit),
        (Roles.TelemarketingId, Screens.Promociones, AccessLevel.View),
        (Roles.TelemarketingId, Screens.Pedidos, AccessLevel.View),

        (Roles.PlantaId, Screens.TicketsEnPlanta, AccessLevel.Edit),
        (Roles.PlantaId, Screens.Pedidos, AccessLevel.View),
    ];
}
