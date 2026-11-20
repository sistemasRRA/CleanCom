using CleanCom.Domain.Access;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Crm;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
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
    DbSet<ParadaFija> ParadasFijas { get; }
    DbSet<ParadaFijaException> ParadaFijaExceptions { get; }
    DbSet<Viaje> Viajes { get; }
    DbSet<PedidoEvento> PedidoEventos { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<Evidence> Evidence { get; }
    DbSet<ReferencePayment> ReferencePayments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// User making the request (null in background jobs or anonymous requests).
/// </summary>
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
