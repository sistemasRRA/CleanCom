using System.Text;
using CleanCom.Application.Access;
using CleanCom.Application.Common;
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
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddSingleton<IPermissionStore, PermissionStore>();
        return services;
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
