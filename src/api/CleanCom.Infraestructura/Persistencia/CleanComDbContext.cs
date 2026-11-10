using CleanCom.Aplicacion.Comun;
using CleanCom.Dominio.Acceso;
using CleanCom.Dominio.Catalogos;
using CleanCom.Dominio.Operacion;
using CleanCom.Dominio.Personas;
using CleanCom.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Infraestructura.Persistencia;

public class CleanComDbContext(DbContextOptions<CleanComDbContext> options)
    : IdentityDbContext<Usuario, Rol, int>(options), ICleanComDbContext
{
    public DbSet<Configuracion> Configuraciones => Set<Configuracion>();
    public DbSet<Ruta> Rutas => Set<Ruta>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<ZonaDiaVisita> ZonasDiasVisita => Set<ZonaDiaVisita>();
    public DbSet<CoberturaCP> Coberturas => Set<CoberturaCP>();
    public DbSet<Lugar> Lugares => Set<Lugar>();
    public DbSet<TipoParada> TiposParada => Set<TipoParada>();
    public DbSet<LineaServicio> LineasServicio => Set<LineaServicio>();
    public DbSet<DiaInhabil> DiasInhabiles => Set<DiaInhabil>();
    public DbSet<MotivoCatalogo> Motivos => Set<MotivoCatalogo>();
    public DbSet<Operador> Operadores => Set<Operador>();
    public DbSet<PermisoRol> PermisosRol => Set<PermisoRol>();
    public DbSet<TokenRenovacion> TokensRenovacion => Set<TokenRenovacion>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Los enums se guardan como texto para que la base se pueda leer sin el código.
        builder.Properties<TipoValorConfiguracion>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<TipoLugar>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<TipoMotivo>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<NivelAcceso>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<DayOfWeek>().HaveConversion<string>().HaveMaxLength(10);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.Property(u => u.NombreCompleto).HasMaxLength(150);
        });
        builder.Entity<Rol>().ToTable("Roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("UsuariosRoles");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UsuariosClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UsuariosLogins");
        builder.Entity<IdentityUserToken<int>>().ToTable("UsuariosTokens");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RolesClaims");

        builder.ApplyConfigurationsFromAssembly(typeof(CleanComDbContext).Assembly);
    }
}
