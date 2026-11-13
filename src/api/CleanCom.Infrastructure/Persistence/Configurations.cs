using CleanCom.Domain.Access;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Crm;
using CleanCom.Domain.Geography;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
using CleanCom.Domain.Routing;
using CleanCom.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanCom.Infrastructure.Persistence;

/// <summary>
/// Fixed date for seed data, so migrations do not change on every build.
/// </summary>
internal static class Seed
{
    public static readonly DateTimeOffset Date = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}

internal class SettingConfig : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> e)
    {
        e.ToTable("Settings");
        e.Property(x => x.Key).HasMaxLength(100);
        e.Property(x => x.Value).HasMaxLength(500);
        e.Property(x => x.Note).HasMaxLength(500);
        e.HasIndex(x => new { x.Key, x.EffectiveFrom });

        e.HasData(SettingCatalog.Definitions.Select((d, i) => new Setting
        {
            Id = i + 1,
            Key = d.Key,
            Type = d.Type,
            Value = d.InitialValue,
            EffectiveFrom = Seed.Date,
            CreatedAt = Seed.Date,
            Note = "Valor inicial",
        }));
    }
}

internal class RutaConfig : IEntityTypeConfiguration<Ruta>
{
    public void Configure(EntityTypeBuilder<Ruta> e)
    {
        e.ToTable("Rutas");
        e.Property(x => x.Code).HasMaxLength(10);
        e.Property(x => x.Name).HasMaxLength(100);
        e.Property(x => x.Color).HasMaxLength(7);
        e.HasIndex(x => x.Code).IsUnique();
    }
}

internal class ZonaConfig : IEntityTypeConfiguration<Zona>
{
    public void Configure(EntityTypeBuilder<Zona> e)
    {
        e.ToTable("Zonas");
        e.Property(x => x.Code).HasMaxLength(20);
        e.Property(x => x.Polygon).HasColumnType("geography");
        e.HasIndex(x => x.Code).IsUnique();
        e.HasOne(x => x.Ruta).WithMany(r => r.Zonas).HasForeignKey(x => x.RutaId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.ZonaNucleo).WithMany().HasForeignKey(x => x.ZonaNucleoId).OnDelete(DeleteBehavior.Restrict);
        e.HasMany(x => x.VisitDays).WithOne().HasForeignKey(d => d.ZonaId).OnDelete(DeleteBehavior.Cascade);
        e.Ignore(x => x.IsExtension);
    }
}

internal class ZonaVisitDayConfig : IEntityTypeConfiguration<ZonaVisitDay>
{
    public void Configure(EntityTypeBuilder<ZonaVisitDay> e)
    {
        e.ToTable("ZonaVisitDays");
        e.HasIndex(x => new { x.ZonaId, x.DayOfWeek }).IsUnique();
    }
}

internal class PostalCodeCoverageConfig : IEntityTypeConfiguration<PostalCodeCoverage>
{
    public void Configure(EntityTypeBuilder<PostalCodeCoverage> e)
    {
        e.ToTable("PostalCodeCoverage");
        e.Property(x => x.PostalCode).HasMaxLength(5).IsFixedLength();
        e.Property(x => x.Neighborhood).HasMaxLength(150);
        e.Property(x => x.Borough).HasMaxLength(100);
        e.Property(x => x.Active).HasDefaultValue(true);
        // Unique among active rows: a colonia given de baja can be added again.
        e.HasIndex(x => new { x.PostalCode, x.Neighborhood }).IsUnique().HasFilter("[active] = 1");
        e.HasOne(x => x.Zona).WithMany().HasForeignKey(x => x.ZonaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal class LugarConfig : IEntityTypeConfiguration<Lugar>
{
    public void Configure(EntityTypeBuilder<Lugar> e)
    {
        e.ToTable("Lugares");
        e.Property(x => x.Name).HasMaxLength(150);
        e.Property(x => x.Address).HasMaxLength(300);
        e.Property(x => x.OpeningHours).HasMaxLength(200);
        e.Property(x => x.Location).HasColumnType("geography");

        e.HasData(new
        {
            Id = 1,
            Name = "Leones",
            Type = LugarType.Base,
            Address = "Calz. de los Leones 165, Los Alpes, Álvaro Obregón, 01710",
            Location = Geo.CreatePoint(19.3608092, -99.1968494),
            Active = true,
            CreatedAt = Seed.Date,
        });
    }
}

internal class ParadaTypeConfig : IEntityTypeConfiguration<ParadaType>
{
    public void Configure(EntityTypeBuilder<ParadaType> e)
    {
        e.ToTable("ParadaTypes");
        e.Property(x => x.Name).HasMaxLength(100);
        e.Property(x => x.RequiredFields).HasMaxLength(1000);
        e.HasIndex(x => x.Name).IsUnique();

        e.HasData(
            new ParadaType { Id = 1, Name = "Convenio", CreatedAt = Seed.Date },
            new ParadaType { Id = 2, Name = "Insumos", CreatedAt = Seed.Date },
            new ParadaType { Id = 3, Name = "Traspaso", CreatedAt = Seed.Date });
    }
}

internal class LineaServicioConfig : IEntityTypeConfiguration<LineaServicio>
{
    public void Configure(EntityTypeBuilder<LineaServicio> e)
    {
        e.ToTable("LineasServicio");
        e.Property(x => x.Name).HasMaxLength(100);
        e.HasIndex(x => x.Name).IsUnique();

        // Processing days per línea are pending confirmation (docs/09-pendientes.md); meanwhile Planta.DiasProceso applies.
        e.HasData(
            new LineaServicio { Id = 1, Name = "Tintorería", CreatedAt = Seed.Date },
            new LineaServicio { Id = 2, Name = "Lavandería", CreatedAt = Seed.Date },
            new LineaServicio { Id = 3, Name = "Planchaduría", CreatedAt = Seed.Date },
            new LineaServicio { Id = 4, Name = "Sastrería", CreatedAt = Seed.Date },
            new LineaServicio { Id = 5, Name = "Calzado", CreatedAt = Seed.Date });
    }
}

internal class NonWorkingDayConfig : IEntityTypeConfiguration<NonWorkingDay>
{
    public void Configure(EntityTypeBuilder<NonWorkingDay> e)
    {
        e.ToTable("NonWorkingDays");
        e.Property(x => x.Reason).HasMaxLength(200);
        e.Property(x => x.Active).HasDefaultValue(true);
        // Unique among active rows: a day given de baja can be marked again.
        e.HasIndex(x => x.Date).IsUnique().HasFilter("[active] = 1");
    }
}

internal class ReasonConfig : IEntityTypeConfiguration<Reason>
{
    public void Configure(EntityTypeBuilder<Reason> e)
    {
        e.ToTable("Reasons");
        e.Property(x => x.Text).HasMaxLength(200);
        e.HasIndex(x => new { x.Type, x.Text }).IsUnique();
    }
}

internal class OperadorConfig : IEntityTypeConfiguration<Operador>
{
    public void Configure(EntityTypeBuilder<Operador> e)
    {
        e.ToTable("Operadores");
        e.Property(x => x.Name).HasMaxLength(150);
        e.Property(x => x.Phone).HasMaxLength(20);
        e.HasOne(x => x.UsualRuta).WithMany().HasForeignKey(x => x.UsualRutaId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => x.UserId).IsUnique().HasFilter("[user_id] IS NOT NULL");
    }
}

internal class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> e)
    {
        e.Property(x => x.DisplayName).HasMaxLength(60);
        e.HasData(Roles.System.Select(r => new Role
        {
            Id = r.Id,
            Name = r.Key,
            NormalizedName = r.Key.ToUpperInvariant(),
            DisplayName = r.Name,
            ConcurrencyStamp = $"role-{r.Key.ToLowerInvariant()}",
        }));
    }
}

internal class RolePermissionConfig : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> e)
    {
        e.ToTable("RolePermissions");
        e.Property(x => x.Screen).HasMaxLength(50);
        e.HasIndex(x => new { x.RoleId, x.Screen }).IsUnique();
        e.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);

        e.HasData(Permissions.Initial.Select((p, i) => new RolePermission
        {
            Id = i + 1,
            RoleId = p.RoleId,
            Screen = p.Screen,
            Level = p.Level,
            CreatedAt = Seed.Date,
        }));
    }
}

internal class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> e)
    {
        e.ToTable("RefreshTokens");
        e.Property(x => x.TokenHash).HasMaxLength(64);
        e.HasIndex(x => x.TokenHash).IsUnique();
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal class InvitadoConfig : IEntityTypeConfiguration<Invitado>
{
    public void Configure(EntityTypeBuilder<Invitado> e)
    {
        e.ToTable("Invitados");
        e.Property(x => x.Name).HasMaxLength(150);
        e.Property(x => x.WhatsAppPhone).HasMaxLength(10).IsFixedLength();
        e.Property(x => x.OtherPhone).HasMaxLength(20);
        e.Property(x => x.Email).HasMaxLength(150);
        e.Property(x => x.HowFoundUs).HasMaxLength(100);
        e.Property(x => x.Campaign).HasMaxLength(100);
        e.Property(x => x.Notes).HasMaxLength(1000);
        // One active ficha per WhatsApp: the app signs in with it and the office searches by it.
        // Invitados given de baja keep their number, and it can be captured again.
        e.HasIndex(x => x.WhatsAppPhone).IsUnique().HasFilter("[active] = 1");
        e.HasIndex(x => x.Name);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => x.UserId).IsUnique().HasFilter("[user_id] IS NOT NULL");
        e.HasMany(x => x.Direcciones).WithOne(d => d.Invitado).HasForeignKey(d => d.InvitadoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal class DireccionConfig : IEntityTypeConfiguration<Direccion>
{
    public void Configure(EntityTypeBuilder<Direccion> e)
    {
        e.ToTable("Direcciones");
        e.Property(x => x.Label).HasMaxLength(50);
        e.Property(x => x.Street).HasMaxLength(200);
        e.Property(x => x.ExteriorNumber).HasMaxLength(20);
        e.Property(x => x.InteriorNumber).HasMaxLength(20);
        e.Property(x => x.PostalCode).HasMaxLength(5).IsFixedLength();
        e.Property(x => x.Neighborhood).HasMaxLength(150);
        e.Property(x => x.Borough).HasMaxLength(100);
        e.Property(x => x.References).HasMaxLength(500);
        e.Property(x => x.Location).HasColumnType("geography");
        e.HasOne(x => x.Zona).WithMany().HasForeignKey(x => x.ZonaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal class PedidoConfig : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> e)
    {
        e.ToTable("Pedidos");
        // Rows created before baja lógica existed start active.
        e.Property(x => x.Active).HasDefaultValue(true);
        e.Property(x => x.DriverNotes).HasMaxLength(500);
        e.HasOne(x => x.Invitado).WithMany().HasForeignKey(x => x.InvitadoId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Direccion).WithMany().HasForeignKey(x => x.DireccionId).OnDelete(DeleteBehavior.Restrict);
        e.HasMany(x => x.LineasServicio).WithOne().HasForeignKey(l => l.PedidoId).OnDelete(DeleteBehavior.Cascade);
        e.HasIndex(x => new { x.Etapa, x.RecoleccionDate });
    }
}

internal class PedidoLineaServicioConfig : IEntityTypeConfiguration<PedidoLineaServicio>
{
    public void Configure(EntityTypeBuilder<PedidoLineaServicio> e)
    {
        e.ToTable("PedidoLineasServicio");
        e.HasKey(x => new { x.PedidoId, x.LineaServicioId });
        e.HasOne(x => x.LineaServicio).WithMany().HasForeignKey(x => x.LineaServicioId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal class RutaDiaConfig : IEntityTypeConfiguration<RutaDia>
{
    public void Configure(EntityTypeBuilder<RutaDia> e)
    {
        e.ToTable("RutaDias");
        // One per ruta and date; the agenda lock relies on it.
        e.HasIndex(x => new { x.RutaId, x.Date }).IsUnique();
        e.HasOne(x => x.Ruta).WithMany().HasForeignKey(x => x.RutaId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Operador).WithMany().HasForeignKey(x => x.OperadorId).OnDelete(DeleteBehavior.Restrict);
        e.HasMany(x => x.Paradas).WithOne(p => p.RutaDia).HasForeignKey(p => p.RutaDiaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal class ParadaConfig : IEntityTypeConfiguration<Parada>
{
    public void Configure(EntityTypeBuilder<Parada> e)
    {
        e.ToTable("Paradas");
        // Rows created before baja lógica existed start active.
        e.Property(x => x.Active).HasDefaultValue(true);
        e.Property(x => x.Notes).HasMaxLength(500);
        e.HasOne(x => x.Pedido).WithMany().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Direccion).WithMany().HasForeignKey(x => x.DireccionId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Lugar).WithMany().HasForeignKey(x => x.LugarId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.RutaDiaId, x.Kind });
    }
}

internal class PostalCodeConfig : IEntityTypeConfiguration<PostalCode>
{
    public void Configure(EntityTypeBuilder<PostalCode> e)
    {
        e.ToTable("PostalCodes");
        e.Property(x => x.Code).HasMaxLength(5).IsFixedLength();
        e.Property(x => x.Settlement).HasMaxLength(150);
        e.Property(x => x.SettlementType).HasMaxLength(50);
        e.Property(x => x.Borough).HasMaxLength(100);
        e.Property(x => x.City).HasMaxLength(100);
        e.Property(x => x.State).HasMaxLength(60);
        e.Property(x => x.StateCode).HasMaxLength(2);
        e.Property(x => x.BoroughCode).HasMaxLength(3);
        e.Property(x => x.SettlementId).HasMaxLength(4);
        e.Property(x => x.AreaType).HasMaxLength(20);
        // How Correos de México identifies a settlement; a new import updates it instead of duplicating it.
        e.HasIndex(x => new { x.StateCode, x.BoroughCode, x.SettlementId }).IsUnique();
        e.HasIndex(x => x.Code);
    }
}

internal class ContactoConfig : IEntityTypeConfiguration<Contacto>
{
    public void Configure(EntityTypeBuilder<Contacto> e)
    {
        e.ToTable("Contactos");
        e.Property(x => x.Note).HasMaxLength(1000);
        e.HasOne(x => x.Invitado).WithMany().HasForeignKey(x => x.InvitadoId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.InvitadoId, x.ContactedAt });
    }
}
