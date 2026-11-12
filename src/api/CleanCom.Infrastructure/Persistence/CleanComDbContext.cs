using CleanCom.Application.Common;
using CleanCom.Domain.Access;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
using CleanCom.Domain.Routing;
using CleanCom.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Infrastructure.Persistence;

public class CleanComDbContext(DbContextOptions<CleanComDbContext> options)
    : IdentityDbContext<User, Role, int>(options), ICleanComDbContext
{
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<Ruta> Rutas => Set<Ruta>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<ZonaVisitDay> ZonaVisitDays => Set<ZonaVisitDay>();
    public DbSet<PostalCodeCoverage> Coverage => Set<PostalCodeCoverage>();
    public DbSet<Lugar> Lugares => Set<Lugar>();
    public DbSet<ParadaType> ParadaTypes => Set<ParadaType>();
    public DbSet<LineaServicio> LineasServicio => Set<LineaServicio>();
    public DbSet<NonWorkingDay> NonWorkingDays => Set<NonWorkingDay>();
    public DbSet<Reason> Reasons => Set<Reason>();
    public DbSet<Operador> Operadores => Set<Operador>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Invitado> Invitados => Set<Invitado>();
    public DbSet<Direccion> Direcciones => Set<Direccion>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<RutaDia> RutaDias => Set<RutaDia>();
    public DbSet<Parada> Paradas => Set<Parada>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Enums are stored as text so the database can be read without the code.
        builder.Properties<SettingValueType>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<LugarType>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<ReasonType>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<AccessLevel>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<DayOfWeek>().HaveConversion<string>().HaveMaxLength(10);
        builder.Properties<InvitadoType>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<PedidoEtapa>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<PedidoOrigin>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<Turno>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<ParadaKind>().HaveConversion<string>().HaveMaxLength(30);
        builder.Properties<ParadaState>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Property(u => u.FullName).HasMaxLength(150);
        });
        builder.Entity<Role>().ToTable("Roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");

        builder.ApplyConfigurationsFromAssembly(typeof(CleanComDbContext).Assembly);

        // Last, so it also renames what Identity and the configurations above defined.
        SnakeCaseNaming.Apply(builder);
    }
}
