using CleanCom.Dominio.Acceso;
using CleanCom.Dominio.Catalogos;
using CleanCom.Dominio.Operacion;
using CleanCom.Dominio.Personas;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Aplicacion.Comun;

/// <summary>
/// Acceso a datos que usan los casos de uso. Lo implementa el DbContext de Infraestructura.
/// </summary>
public interface ICleanComDbContext
{
    DbSet<Configuracion> Configuraciones { get; }
    DbSet<Ruta> Rutas { get; }
    DbSet<Zona> Zonas { get; }
    DbSet<ZonaDiaVisita> ZonasDiasVisita { get; }
    DbSet<CoberturaCP> Coberturas { get; }
    DbSet<Lugar> Lugares { get; }
    DbSet<TipoParada> TiposParada { get; }
    DbSet<LineaServicio> LineasServicio { get; }
    DbSet<DiaInhabil> DiasInhabiles { get; }
    DbSet<MotivoCatalogo> Motivos { get; }
    DbSet<Operador> Operadores { get; }
    DbSet<PermisoRol> PermisosRol { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Usuario que hace la petición (null en procesos programados o peticiones anónimas).
/// </summary>
public interface IUsuarioActual
{
    int? Id { get; }
}

/// <summary>404: no existe lo que se pidió.</summary>
public class ExcepcionNoEncontrado(string mensaje) : Exception(mensaje);

/// <summary>409: choca con algo que ya existe.</summary>
public class ExcepcionConflicto(string mensaje) : Exception(mensaje);

/// <summary>401: credenciales o token no válidos.</summary>
public class ExcepcionNoAutorizado(string mensaje) : Exception(mensaje);
