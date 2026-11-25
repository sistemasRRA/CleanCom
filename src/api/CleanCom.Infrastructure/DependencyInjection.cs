using System.Text;
using CleanCom.Application.Access;
using CleanCom.Application.Agenda;
using CleanCom.Application.Common;
using CleanCom.Application.Geography;
using CleanCom.Domain.Access;
using CleanCom.Domain.Time;
using CleanCom.Infrastructure.Identity;
using CleanCom.Infrastructure.Persistence;
using CleanCom.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CleanCom.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing ConnectionStrings:DefaultConnection. Set it with dotnet user-secrets or the ConnectionStrings__DefaultConnection variable.");
        }

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IBusinessClock, BusinessClock>();

        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<CleanComDbContext>((sp, options) => options
            .UseSqlServer(connectionString, sql => sql
                .UseNetTopologySuite()
                // Migrations live in database/migrations (CleanCom.Migrations project).
                .MigrationsAssembly("CleanCom.Migrations")
                .MigrationsHistoryTable("__ef_migrations_history"))
            .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
        services.AddScoped<ICleanComDbContext>(sp => sp.GetRequiredService<CleanComDbContext>());
        services.AddScoped<IAgendaLock, AgendaLock>();
        services.AddScoped<IBulkInserter, BulkInserter>();
        services.AddSingleton<IFileStore, Files.LocalFileStore>();

        // Map of the address form: OpenStreetMap's Nominatim until the Google Cloud keys exist.
        services.AddMemoryCache();
        services.AddHttpClient<IGeocoder, Geography.NominatimGeocoder>(http =>
        {
            http.BaseAddress = new Uri(configuration["Geocoding:Nominatim:Url"] ?? "https://nominatim.openstreetmap.org/");
            // Nominatim asks for a User-Agent that identifies the application (with a contact in production).
            http.DefaultRequestHeaders.UserAgent.ParseAdd(configuration["Geocoding:UserAgent"] ?? "CleanCom/1.0");
            http.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddIdentityCore<User>(o =>
            {
                // Docs: passwords need at least 8 characters, nothing else.
                o.Password.RequiredLength = 8;
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredUniqueChars = 1;
                o.User.RequireUniqueEmail = false;
                o.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._@-";
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<CleanComDbContext>()
            .AddErrorDescriber<SpanishIdentityErrorDescriber>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(o => Encoding.UTF8.GetByteCount(o.Key) >= 32, "Jwt:Key needs at least 32 characters. Set it with dotnet user-secrets or the Jwt__Key variable.")
            .ValidateOnStart();

        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        // The code goes by WhatsApp once Meta is connected (week 6); in development it is written to the log.
        if (configuration.GetValue<bool>("AccessCodes:LogToConsole"))
        {
            services.AddSingleton<IAccessCodeSender, Messaging.LoggingAccessCodeSender>();
        }
        else
        {
            services.AddSingleton<IAccessCodeSender, Messaging.UnavailableAccessCodeSender>();
        }
        // Avisos: test senders until Meta (WhatsApp) and the app's EAS project (push) exist (docs/04-integraciones.md).
        services.AddSingleton<CleanCom.Application.Avisos.IAvisoSender, Messaging.TestWhatsAppSender>();
        services.AddSingleton<CleanCom.Application.Avisos.IAvisoSender, Messaging.TestPushSender>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddSingleton<IPermissionStore, PermissionStore>();
        return services;
    }

    /// <summary>
    /// Initial options of editable catalogs that start with content: "Cómo nos conoció", the avisos templates and the
    /// promotions of the demo (sellos and cuponera). Inserted only when the catalog has no row of that type (active or not), so the office's
    /// edits are never undone. Not done with HasData because Reasons already has rows captured by hand and fixed ids
    /// would clash with them.
    /// </summary>
    public static async Task SeedCatalogsAsync(this IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CleanComDbContext>();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
        if (!await db.Reasons.AnyAsync(r => r.Type == Domain.Catalogs.ReasonType.HowFoundUs))
        {
            db.Reasons.AddRange(Domain.Catalogs.Reason.HowFoundUsDefaults.Select(text =>
                new Domain.Catalogs.Reason { Type = Domain.Catalogs.ReasonType.HowFoundUs, Text = text }));
            await db.SaveChangesAsync();
            log.LogInformation("Catalog \"Cómo nos conoció\" created with {Count} options", Domain.Catalogs.Reason.HowFoundUsDefaults.Length);
        }

        // Avisos: one template per event, created when its event has none (new events get theirs on the next start).
        var withTemplate = await db.Plantillas.Select(p => p.Evento).ToListAsync();
        var missing = Domain.Avisos.AvisoRules.Defaults().Where(p => !withTemplate.Contains(p.Evento)).ToList();
        if (missing.Count > 0)
        {
            db.Plantillas.AddRange(missing);
            await db.SaveChangesAsync();
            log.LogInformation("Avisos: {Count} templates created", missing.Count);
        }

        // Promotions of the demo, per program, only while that program has none (active or not).
        foreach (var (programa, defaults) in new[]
                 {
                     (Domain.Promociones.PromocionPrograma.Sellos, Domain.Promociones.PromocionDefaults.Sellos()),
                     (Domain.Promociones.PromocionPrograma.Cuponera, Domain.Promociones.PromocionDefaults.Cuponera()),
                 })
        {
            if (await db.Promociones.AnyAsync(p => p.Programa == programa))
            {
                continue;
            }
            db.Promociones.AddRange(defaults);
            await db.SaveChangesAsync();
            log.LogInformation("Promotions {Programa} created with {Count} rows", programa, defaults.Count);
        }
    }

    /// <summary>
    /// Creates the first SuperUser user when there is none and Seed:Admin:UserName and
    /// Seed:Admin:Password are configured. The password must be changed on first sign-in.
    /// </summary>
    public static async Task SeedAdministratorAsync(this IServiceProvider provider, IConfiguration configuration)
    {
        var section = configuration.GetSection("Seed:Admin");
        var userName = section["UserName"];
        var password = section["Password"];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        using var scope = provider.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
        try
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            if ((await users.GetUsersInRoleAsync(Roles.SuperUser)).Count > 0)
            {
                return;
            }

            // Databases created before SuperUser existed: the seeded user becomes SuperUser so
            // someone can still open Roles y permisos.
            var existing = await users.FindByNameAsync(userName);
            if (existing is not null)
            {
                var current = await users.GetRolesAsync(existing);
                IdentityErrors.ThrowIfFailed(await users.RemoveFromRolesAsync(existing, current));
                IdentityErrors.ThrowIfFailed(await users.AddToRoleAsync(existing, Roles.SuperUser));
                log.LogInformation("User {UserName} moved to SuperUser", userName);
                return;
            }

            var user = new User
            {
                UserName = userName,
                FullName = section["FullName"] ?? "Administración",
                MustChangePassword = true,
                CreatedAt = scope.ServiceProvider.GetRequiredService<IBusinessClock>().UtcNow,
            };
            IdentityErrors.ThrowIfFailed(await users.CreateAsync(user, password));
            IdentityErrors.ThrowIfFailed(await users.AddToRoleAsync(user, Roles.SuperUser));
            log.LogInformation("Created SuperUser user {UserName}", userName);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Could not create the initial SuperUser user. Have the migrations been applied?");
        }
    }
}
