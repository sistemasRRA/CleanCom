using System.Security.Claims;
using CleanCom.Aplicacion.Comun;
using CleanCom.Dominio.Acceso;
using CleanCom.Infraestructura.Identidad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CleanCom.Api.Infraestructura;

public static class Politicas
{
    /// <summary>Basta con haber iniciado sesión, aunque falte cambiar la contraseña inicial.</summary>
    public const string SesionIniciada = "SesionIniciada";

    public const string PrefijoPantalla = "Pantalla:";

    public static string Pantalla(string pantalla, NivelAcceso nivel) => $"{PrefijoPantalla}{pantalla}:{nivel}";
}

/// <summary>
/// Políticas "Pantalla:{pantalla}:{nivel}" creadas al vuelo a partir de los permisos del token.
/// </summary>
public class ProveedorPoliticas(IOptions<AuthorizationOptions> opciones) : DefaultAuthorizationPolicyProvider(opciones)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string nombre)
    {
        if (!nombre.StartsWith(Politicas.PrefijoPantalla, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(nombre);
        }

        var partes = nombre[Politicas.PrefijoPantalla.Length..].Split(':');
        if (partes.Length != 2 || !Enum.TryParse<NivelAcceso>(partes[1], out var nivel))
        {
            return null;
        }
        var pantalla = partes[0];

        return Seguridad.ConSesionActiva(new AuthorizationPolicyBuilder())
            .RequireAssertion(ctx => Permisos.Permite(
                ctx.User.FindAll(ClaimsCleanCom.Permiso).Select(c => c.Value), pantalla, nivel))
            .Build();
    }
}

/// <summary>
/// Pide un permiso de pantalla en un controlador o acción:
/// <c>[RequierePantalla(Pantallas.Operacion, NivelAcceso.Editar)]</c>. Editar incluye Ver.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequierePantallaAttribute(string pantalla, NivelAcceso nivel)
    : AuthorizeAttribute(Politicas.Pantalla(pantalla, nivel))
{
    public string Pantalla { get; } = pantalla;

    public NivelAcceso Nivel { get; } = nivel;
}

public class UsuarioActual(IHttpContextAccessor accesor) : IUsuarioActual
{
    public int? Id => accesor.HttpContext?.User.IdUsuario();
}

public static class Seguridad
{
    public static int? IdUsuario(this ClaimsPrincipal usuario) =>
        int.TryParse(usuario.FindFirst(ClaimsCleanCom.Sujeto)?.Value, out var id) ? id : null;

    /// <summary>Sesión iniciada y contraseña inicial ya cambiada.</summary>
    public static AuthorizationPolicyBuilder ConSesionActiva(AuthorizationPolicyBuilder politica) =>
        politica
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx => !ctx.User.HasClaim(c => c.Type == ClaimsCleanCom.DebeCambiarContrasena));

    public static IServiceCollection AddSeguridad(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioActual, UsuarioActual>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<OpcionesJwt>>((o, jwt) =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Emisor,
                    ValidAudience = jwt.Value.Audiencia,
                    IssuerSigningKey = jwt.Value.LlaveFirma(),
                    NameClaimType = ClaimsCleanCom.Usuario,
                    RoleClaimType = ClaimsCleanCom.Rol,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddSingleton<IAuthorizationPolicyProvider, ProveedorPoliticas>();
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(ConSesionActiva(new AuthorizationPolicyBuilder()).Build())
            // Todo endpoint pide sesión salvo los marcados con AllowAnonymous.
            .SetFallbackPolicy(ConSesionActiva(new AuthorizationPolicyBuilder()).Build())
            .AddPolicy(Politicas.SesionIniciada, p => p
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());
        return services;
    }
}
