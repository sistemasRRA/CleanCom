using CleanCom.Dominio.Acceso;
using CleanCom.Dominio.Catalogos;
using CleanCom.Dominio.Geografia;
using CleanCom.Dominio.Operacion;
using CleanCom.Dominio.Personas;
using CleanCom.Infraestructura.Identidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanCom.Infraestructura.Persistencia;

/// <summary>
/// Fecha fija de los datos iniciales, para que las migraciones no cambien en cada compilación.
/// </summary>
internal static class Semilla
{
    public static readonly DateTimeOffset Fecha = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}

internal class ConfiguracionConfig : IEntityTypeConfiguration<Configuracion>
{
    public void Configure(EntityTypeBuilder<Configuracion> e)
    {
        e.ToTable("Configuraciones");
        e.Property(x => x.Clave).HasMaxLength(100);
        e.Property(x => x.Valor).HasMaxLength(500);
        e.Property(x => x.Nota).HasMaxLength(500);
        e.HasIndex(x => new { x.Clave, x.VigenteDesde });

        e.HasData(CatalogoConfiguracion.Definiciones.Select((d, i) => new Configuracion
        {
            Id = i + 1,
            Clave = d.Clave,
            Tipo = d.Tipo,
            Valor = d.ValorInicial,
            VigenteDesde = Semilla.Fecha,
            CreadoEn = Semilla.Fecha,
            Nota = "Valor inicial",
        }));
    }
}

internal class RutaConfig : IEntityTypeConfiguration<Ruta>
{
    public void Configure(EntityTypeBuilder<Ruta> e)
    {
        e.ToTable("Rutas");
        e.Property(x => x.Codigo).HasMaxLength(10);
        e.Property(x => x.Nombre).HasMaxLength(100);
        e.Property(x => x.Color).HasMaxLength(7);
        e.HasIndex(x => x.Codigo).IsUnique();
    }
}

internal class ZonaConfig : IEntityTypeConfiguration<Zona>
{
    public void Configure(EntityTypeBuilder<Zona> e)
    {
        e.ToTable("Zonas");
        e.Property(x => x.Codigo).HasMaxLength(20);
        e.Property(x => x.Poligono).HasColumnType("geography");
        e.HasIndex(x => x.Codigo).IsUnique();
        e.HasOne(x => x.Ruta).WithMany(r => r.Zonas).HasForeignKey(x => x.RutaId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.ZonaNucleo).WithMany().HasForeignKey(x => x.ZonaNucleoId).OnDelete(DeleteBehavior.Restrict);
        e.HasMany(x => x.DiasVisita).WithOne().HasForeignKey(d => d.ZonaId).OnDelete(DeleteBehavior.Cascade);
        e.Ignore(x => x.EsExtension);
    }
}

internal class ZonaDiaVisitaConfig : IEntityTypeConfiguration<ZonaDiaVisita>
{
    public void Configure(EntityTypeBuilder<ZonaDiaVisita> e)
    {
        e.ToTable("ZonasDiasVisita");
        e.HasIndex(x => new { x.ZonaId, x.DiaSemana }).IsUnique();
    }
}

internal class CoberturaConfig : IEntityTypeConfiguration<CoberturaCP>
{
    public void Configure(EntityTypeBuilder<CoberturaCP> e)
    {
        e.ToTable("CoberturasCP");
        e.Property(x => x.CodigoPostal).HasMaxLength(5).IsFixedLength();
        e.Property(x => x.Colonia).HasMaxLength(150);
        e.Property(x => x.Alcaldia).HasMaxLength(100);
        e.HasIndex(x => new { x.CodigoPostal, x.Colonia }).IsUnique();
        e.HasOne(x => x.Zona).WithMany().HasForeignKey(x => x.ZonaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal class LugarConfig : IEntityTypeConfiguration<Lugar>
{
    public void Configure(EntityTypeBuilder<Lugar> e)
    {
        e.ToTable("Lugares");
        e.Property(x => x.Nombre).HasMaxLength(150);
        e.Property(x => x.Direccion).HasMaxLength(300);
        e.Property(x => x.Horario).HasMaxLength(200);
        e.Property(x => x.Ubicacion).HasColumnType("geography");

        e.HasData(new
        {
            Id = 1,
            Nombre = "Leones",
            Tipo = TipoLugar.Base,
            Direccion = "Calz. de los Leones 165, Los Alpes, Álvaro Obregón, 01710",
            Ubicacion = Geo.Punto(19.3608092, -99.1968494),
            Activo = true,
            CreadoEn = Semilla.Fecha,
        });
    }
}

internal class TipoParadaConfig : IEntityTypeConfiguration<TipoParada>
{
    public void Configure(EntityTypeBuilder<TipoParada> e)
    {
        e.ToTable("TiposParada");
        e.Property(x => x.Nombre).HasMaxLength(100);
        e.Property(x => x.CamposRequeridos).HasMaxLength(1000);
        e.HasIndex(x => x.Nombre).IsUnique();
        e.HasData(
            new TipoParada { Id = 1, Nombre = "Convenio", CreadoEn = Semilla.Fecha },
            new TipoParada { Id = 2, Nombre = "Insumos", CreadoEn = Semilla.Fecha },
            new TipoParada { Id = 3, Nombre = "Traspaso", CreadoEn = Semilla.Fecha });
    }
}

internal class LineaServicioConfig : IEntityTypeConfiguration<LineaServicio>
{
    public void Configure(EntityTypeBuilder<LineaServicio> e)
    {
        e.ToTable("LineasServicio");
        e.Property(x => x.Nombre).HasMaxLength(100);
        e.HasIndex(x => x.Nombre).IsUnique();
        // Días de proceso por línea pendientes de confirmar (docs/09-pendientes.md); mientras, aplica Planta.DiasProceso.
        e.HasData(
            new LineaServicio { Id = 1, Nombre = "Tintorería", CreadoEn = Semilla.Fecha },
            new LineaServicio { Id = 2, Nombre = "Lavandería", CreadoEn = Semilla.Fecha },
            new LineaServicio { Id = 3, Nombre = "Planchaduría", CreadoEn = Semilla.Fecha },
            new LineaServicio { Id = 4, Nombre = "Sastrería", CreadoEn = Semilla.Fecha },
            new LineaServicio { Id = 5, Nombre = "Calzado", CreadoEn = Semilla.Fecha });
    }
}

internal class DiaInhabilConfig : IEntityTypeConfiguration<DiaInhabil>
{
    public void Configure(EntityTypeBuilder<DiaInhabil> e)
    {
        e.ToTable("DiasInhabiles");
        e.Property(x => x.Motivo).HasMaxLength(200);
        e.HasIndex(x => x.Fecha).IsUnique();
    }
}

internal class MotivoConfig : IEntityTypeConfiguration<MotivoCatalogo>
{
    public void Configure(EntityTypeBuilder<MotivoCatalogo> e)
    {
        e.ToTable("MotivosCatalogo");
        e.Property(x => x.Texto).HasMaxLength(200);
        e.HasIndex(x => new { x.Tipo, x.Texto }).IsUnique();
    }
}

internal class OperadorConfig : IEntityTypeConfiguration<Operador>
{
    public void Configure(EntityTypeBuilder<Operador> e)
    {
        e.ToTable("Operadores");
        e.Property(x => x.Nombre).HasMaxLength(150);
        e.Property(x => x.Telefono).HasMaxLength(20);
        e.HasOne(x => x.RutaHabitual).WithMany().HasForeignKey(x => x.RutaHabitualId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => x.UsuarioId).IsUnique().HasFilter("[UsuarioId] IS NOT NULL");
    }
}

internal class RolConfig : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> e)
    {
        e.HasData(
            new Rol { Id = Roles.IdAdministracion, Name = Roles.Administracion, NormalizedName = Roles.Administracion.ToUpperInvariant(), ConcurrencyStamp = "rol-administracion" },
            new Rol { Id = Roles.IdChofer, Name = Roles.Chofer, NormalizedName = Roles.Chofer.ToUpperInvariant(), ConcurrencyStamp = "rol-chofer" },
            new Rol { Id = Roles.IdInvitado, Name = Roles.Invitado, NormalizedName = Roles.Invitado.ToUpperInvariant(), ConcurrencyStamp = "rol-invitado" });
    }
}

internal class PermisoRolConfig : IEntityTypeConfiguration<PermisoRol>
{
    public void Configure(EntityTypeBuilder<PermisoRol> e)
    {
        e.ToTable("PermisosRol");
        e.Property(x => x.Pantalla).HasMaxLength(50);
        e.HasIndex(x => new { x.RolId, x.Pantalla }).IsUnique();
        e.HasOne<Rol>().WithMany().HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Cascade);
        e.HasData(Permisos.Iniciales.Select((p, i) => new PermisoRol
        {
            Id = i + 1,
            RolId = p.RolId,
            Pantalla = p.Pantalla,
            Nivel = p.Nivel,
            CreadoEn = Semilla.Fecha,
        }));
    }
}

internal class TokenRenovacionConfig : IEntityTypeConfiguration<TokenRenovacion>
{
    public void Configure(EntityTypeBuilder<TokenRenovacion> e)
    {
        e.ToTable("TokensRenovacion");
        e.Property(x => x.TokenHash).HasMaxLength(64);
        e.HasIndex(x => x.TokenHash).IsUnique();
        e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
