using CleanCom.Domain.Common;
using CleanCom.Domain.Access;
using CleanCom.Domain.Avisos;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Crm;
using CleanCom.Domain.Fleet;
using CleanCom.Domain.Kpis;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
using CleanCom.Domain.Promociones;
using CleanCom.Domain.Routing;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Common;

/// <summary>
/// Data access used by the use cases. Implemented by the Infrastructure DbContext.
/// </summary>
public interface ICleanComDbContext
{
    DbSet<Setting> Settings { get; }
    DbSet<Ruta> Rutas { get; }
    DbSet<Zona> Zonas { get; }
    DbSet<ZonaVisitDay> ZonaVisitDays { get; }
    DbSet<PostalCodeCoverage> Coverage { get; }
    DbSet<PostalCode> PostalCodes { get; }
    DbSet<Lugar> Lugares { get; }
    DbSet<ParadaType> ParadaTypes { get; }
    DbSet<LineaServicio> LineasServicio { get; }
    DbSet<NonWorkingDay> NonWorkingDays { get; }
    DbSet<Reason> Reasons { get; }
    DbSet<Operador> Operadores { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Invitado> Invitados { get; }
    DbSet<Address> Addresses { get; }
    DbSet<Pedido> Pedidos { get; }
    DbSet<RutaDia> RutaDias { get; }
    DbSet<Parada> Paradas { get; }
    DbSet<Contact> Contacts { get; }
    DbSet<ChangeLog> ChangeLogs { get; }
    DbSet<ParadaFija> ParadasFijas { get; }
    DbSet<ParadaFijaException> ParadaFijaExceptions { get; }
    DbSet<Viaje> Viajes { get; }
    DbSet<PedidoEvento> PedidoEventos { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<Evidence> Evidence { get; }
    DbSet<ReferencePayment> ReferencePayments { get; }
    DbSet<CorteCaja> CortesCaja { get; }
    DbSet<ChoferLocation> ChoferLocations { get; }
    DbSet<Unidad> Unidades { get; }
    DbSet<UnidadDocument> UnidadDocuments { get; }
    DbSet<UnidadEvent> UnidadEvents { get; }
    DbSet<RecurringPayment> RecurringPayments { get; }
    DbSet<FleetAlertRule> FleetAlertRules { get; }
    DbSet<Promocion> Promociones { get; }
    DbSet<Beneficio> Beneficios { get; }
    DbSet<Plantilla> Plantillas { get; }
    DbSet<Aviso> Avisos { get; }
    DbSet<DispositivoPush> DispositivosPush { get; }
    DbSet<AvisoOficina> AvisosOficina { get; }
    DbSet<KpiGoal> KpiGoals { get; }
    DbSet<KpiValue> KpiValues { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Pushes what changes during the day to the office in real time (SignalR hub "operacion"). A failure to push never
/// fails the request: the screens also reload on their own.
/// </summary>
public interface IOperacionNotifier
{
    /// <summary>Where a chofer is now (live map).</summary>
    Task LocationAsync(LiveLocationDto location, CancellationToken ct);

    /// <summary>A parada changed state (en camino, llegué, cerrada, no realizada).</summary>
    Task ParadaAsync(ParadaChangedDto change, CancellationToken ct);
}

/// <summary>The chofer's last known position on a RutaDia.</summary>
/// <param name="Live">Received in this run of the API; false when it comes from the saved history after a restart.</param>
/// <param name="SignalLostAt">
/// When this position stops counting as current if no newer one arrives (<see cref="At"/> + Gps.SinSenalMinutos); the
/// map shows it as old from then on.
/// </param>
public record LiveLocationDto(
    int RutaDiaId,
    DateOnly Date,
    int RutaId,
    string RutaCode,
    string? RutaColor,
    string? OperadorName,
    double Lat,
    double Lng,
    DateTimeOffset At,
    int? AccuracyMeters,
    bool Live = true,
    DateTimeOffset? SignalLostAt = null);

/// <summary>A parada that changed state; the office reloads that ruta.</summary>
public record ParadaChangedDto(int RutaDiaId, DateOnly Date, int ParadaId, CleanCom.Domain.Routing.ParadaState State);

/// <summary>
/// User making the request (null in background jobs or anonymous requests).
/// </summary>
/// <summary>The invitado's user in the app: whether it can sign in and when it last did.</summary>
public record AppUserInfo(bool Active, DateTimeOffset? LastAccess);

/// <summary>The invitados' app accounts (users with the Invitado role), for the CRM.</summary>
public interface IInvitadoAccounts
{
    Task<IReadOnlyDictionary<int, AppUserInfo>> InfoAsync(IReadOnlyCollection<int> userIds, CancellationToken ct);

    /// <summary>Blocks or unblocks the access; blocking also ends the open sessions.</summary>
    Task SetActiveAsync(int userId, bool active, CancellationToken ct);
}

/// <summary>
/// Photos and documents on the server's disk (Archivos:Ruta), never in SQL Server. Paths are relative to that folder.
/// </summary>
public interface IFileStore
{
    /// <summary>Saves the content under <paramref name="folder"/> with a new name and returns its relative path.</summary>
    Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct);

    /// <summary>Opens a saved file; null when it is not there.</summary>
    Stream? OpenRead(string relativePath);
}

/// <summary>
/// Inserts many new rows in one go (SqlBulkCopy), for imports of catalogs like the postal codes. Audit fields are
/// set by the caller; nothing else goes through the change tracker.
/// </summary>
public interface IBulkInserter
{
    Task InsertAsync<T>(IReadOnlyCollection<T> rows, CancellationToken ct) where T : class;
}

public interface ICurrentUser
{
    int? Id { get; }
}

/// <summary>404: what was requested does not exist.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>409: clashes with something that already exists.</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>409: the action needs the user to confirm it (the request is repeated with the confirmation).</summary>
public class ConfirmationRequiredException(string message) : Exception(message);

/// <summary>401: invalid credentials or token.</summary>
public class UnauthorizedException(string message) : Exception(message);

/// <summary>
/// Sends the invitado app's sign-in code (WhatsApp, SMS as backup). Until Meta and Twilio are connected (week 6), the
/// development build writes it to the log.
/// </summary>
public interface IAccessCodeSender
{
    Task SendAsync(string phone, string code, int minutes, CancellationToken ct);
}
