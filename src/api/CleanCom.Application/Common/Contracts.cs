using CleanCom.Domain.Access;
using CleanCom.Domain.Catalogs;
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
    DbSet<Direccion> Direcciones { get; }
    DbSet<Pedido> Pedidos { get; }
    DbSet<RutaDia> RutaDias { get; }
    DbSet<Parada> Paradas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// User making the request (null in background jobs or anonymous requests).
/// </summary>
public interface ICurrentUser
{
    int? Id { get; }
}

/// <summary>404: what was requested does not exist.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>409: clashes with something that already exists.</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>401: invalid credentials or token.</summary>
public class UnauthorizedException(string message) : Exception(message);
