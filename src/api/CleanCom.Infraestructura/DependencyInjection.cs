using System.Text;
using CleanCom.Aplicacion.Acceso;
using CleanCom.Aplicacion.Comun;
using CleanCom.Dominio.Acceso;
using CleanCom.Dominio.Tiempo;
using CleanCom.Infraestructura.Identidad;
using CleanCom.Infraestructura.Persistencia;
using CleanCom.Infraestructura.Tiempo;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CleanCom.Infraestructura;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuracion)
    {
        var cadena = configuracion.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new InvalidOperationException(
                "Falta ConnectionStrings:DefaultConnection. Configúrala con dotnet user-secrets o la variable ConnectionStrings__DefaultConnection.");
        }

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IRelojNegocio, RelojNegocio>();
        services.AddScoped<InterceptorAuditoria>();

        services.AddDbContext<CleanComDbContext>((sp, opciones) => opciones
            .UseSqlServer(cadena, sql => sql
                .UseNetTopologySuite()
                // Las migraciones viven en database/migraciones (proyecto CleanCom.Migraciones).
                .MigrationsAssembly("CleanCom.Migraciones"))
            .AddInterceptors(sp.GetRequiredService<InterceptorAuditoria>()));
        services.AddScoped<ICleanComDbContext>(sp => sp.GetRequiredService<CleanComDbContext>());

        services.AddIdentityCore<Usuario>(o =>
            {
                // Docs: contraseña de mínimo 8 caracteres, sin más requisitos.
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
            .AddRoles<Rol>()
            .AddEntityFrameworkStores<CleanComDbContext>()
            .AddErrorDescriber<DescriptorErroresIdentidad>();

        services.AddOptions<OpcionesJwt>()
            .Bind(configuracion.GetSection("Jwt"))
            .Validate(o => Encoding.UTF8.GetByteCount(o.Llave) >= 32, "Jwt:Llave debe tener al menos 32 caracteres. Configúrala con dotnet user-secrets o la variable Jwt__Llave.")
            .ValidateOnStart();

        services.AddScoped<ServicioTokens>();
        services.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
        services.AddScoped<IServicioUsuarios, ServicioUsuarios>();
        return services;
    }

    /// <summary>
    /// Crea el primer usuario de Administración si no hay ninguno y están configurados
    /// Semilla:Admin:Usuario y Semilla:Admin:Contrasena. Debe cambiar la contraseña al entrar.
    /// </summary>
    public static async Task SembrarAdministradorAsync(this IServiceProvider proveedor, IConfiguration configuracion)
    {
        var seccion = configuracion.GetSection("Semilla:Admin");
        var nombreUsuario = seccion["Usuario"];
        var contrasena = seccion["Contrasena"];
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(contrasena))
        {
            return;
        }

        using var scope = proveedor.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Semilla");
        try
        {
            var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            if ((await usuarios.GetUsersInRoleAsync(Roles.Administracion)).Count > 0)
            {
                return;
            }

            var usuario = new Usuario
            {
                UserName = nombreUsuario,
                NombreCompleto = seccion["NombreCompleto"] ?? "Administración",
                DebeCambiarContrasena = true,
                CreadoEn = scope.ServiceProvider.GetRequiredService<IRelojNegocio>().AhoraUtc,
            };
            ErroresIdentidad.LanzarSiFalla(await usuarios.CreateAsync(usuario, contrasena));
            ErroresIdentidad.LanzarSiFalla(await usuarios.AddToRoleAsync(usuario, Roles.Administracion));
            log.LogInformation("Se creó el usuario de Administración {Usuario}", nombreUsuario);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo crear el usuario de Administración inicial. ¿Ya se aplicaron las migraciones?");
        }
    }
}
