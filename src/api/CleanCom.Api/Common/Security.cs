using System.Security.Claims;
using CleanCom.Application.Access;
using CleanCom.Application.Common;
using CleanCom.Domain.Access;
using CleanCom.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CleanCom.Api.Common;

public static class Policies
{
    /// <summary>Being signed in is enough, even if the initial password has not been changed yet.</summary>
    public const string SignedIn = "SignedIn";

    /// <summary>
    /// SuperUser and Administracion only, fixed in code: for actions that cannot be undone (deleting pedidos and
    /// invitados). Roles y permisos cannot hand it to another role.
    /// </summary>
    public const string Administrators = "Administrators";

    /// <summary>
    /// Capturing an address with the map (GET /direcciones/ubicar): editing any screen that captures addresses.
    /// </summary>
    public const string CapturesAddresses = "CapturesAddresses";

    /// <summary>
    /// The day's live operation (hub "operacion", live locations and recorridos): seeing Panel del día, Rutas or Vista del chofer.
    /// </summary>
    public const string SeesOperacion = "SeesOperacion";

    /// <summary>Choosing the chofer and unidad of a ruta del d�a: editing Panel del d�a or Rutas.</summary>
    public const string AssignsRutaDia = "AssignsRutaDia";

    /// <summary>
    /// The agenda engine (disponibilidad, agendar): the office with Agendar, or the invitado app (App del invitado), which
    /// gets the Invitado role's rules.
    /// </summary>
    public const string UsesAgenda = "UsesAgenda";
    public const string SchedulesAgenda = "SchedulesAgenda";

    public const string ScreenPrefix = "Screen:";

    public static string Screen(string screen, AccessLevel level) => $"{ScreenPrefix}{screen}:{level}";
}

/// <summary>
/// "Screen:{screen}:{level}" policies built on the fly. The permissions come from IPermissionStore,
/// not from the token, so a change in Roles y permisos applies on the next request.
/// </summary>
public class ScreenPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string name)
    {
        if (!name.StartsWith(Policies.ScreenPrefix, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(name);
        }

        var parts = name[Policies.ScreenPrefix.Length..].Split(':');
        if (parts.Length != 2 || !Enum.TryParse<AccessLevel>(parts[1], out var level))
        {
            return null;
        }

        return Security.WithActiveSession(new AuthorizationPolicyBuilder())
            .AddRequirements(new ScreenRequirement(parts[0], level))
            .Build();
    }
}

public record ScreenRequirement(string Screen, AccessLevel Level) : IAuthorizationRequirement;

/// <summary>Any one of the screens at that level is enough (several RequireScreen attributes would need all of them).</summary>
public record AnyScreenRequirement(IReadOnlyList<string> Screens, AccessLevel Level) : IAuthorizationRequirement;

public class AnyScreenRequirementHandler(IPermissionStore store) : AuthorizationHandler<AnyScreenRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AnyScreenRequirement requirement)
    {
        var role = context.User.FindFirst(CleanComClaims.Role)?.Value;
        if (string.IsNullOrEmpty(role))
        {
            return;
        }
        var permissions = await store.GetAsync(role, CancellationToken.None);
        if (requirement.Screens.Any(s => Permissions.Allows(permissions, s, requirement.Level)))
        {
            context.Succeed(requirement);
        }
    }
}

public class ScreenRequirementHandler(IPermissionStore store) : AuthorizationHandler<ScreenRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ScreenRequirement requirement)
    {
        var role = context.User.FindFirst(CleanComClaims.Role)?.Value;
        if (string.IsNullOrEmpty(role))
        {
            return;
        }
        var permissions = await store.GetAsync(role, CancellationToken.None);
        if (Permissions.Allows(permissions, requirement.Screen, requirement.Level))
        {
            context.Succeed(requirement);
        }
    }
}

/// <summary>
/// Requires a screen permission on a controller or action:
/// <c>[RequireScreen(Screens.Operacion, AccessLevel.Edit)]</c>. Edit includes View.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireScreenAttribute(string screen, AccessLevel level)
    : AuthorizeAttribute(Policies.Screen(screen, level))
{
    public string Screen { get; } = screen;
    public AccessLevel Level { get; } = level;
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public int? Id => accessor.HttpContext?.User.GetUserId();
}

public static class Security
{
    public static int? GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirst(CleanComClaims.Subject)?.Value, out var id) ? id : null;

    /// <summary>Signed in and the initial password already changed.</summary>
    public static AuthorizationPolicyBuilder WithActiveSession(AuthorizationPolicyBuilder policy) =>
        policy
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx => !ctx.User.HasClaim(c => c.Type == CleanComClaims.MustChangePassword));

    public static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((o, jwt) =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = jwt.Value.SigningKey(),
                    NameClaimType = CleanComClaims.UserName,
                    RoleClaimType = CleanComClaims.Role,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
                // WebSockets cannot carry the Authorization header: the SignalR client sends the token in the query.
                o.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddSingleton<IAuthorizationPolicyProvider, ScreenPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, ScreenRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, AnyScreenRequirementHandler>();
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(WithActiveSession(new AuthorizationPolicyBuilder()).Build())
            // Every endpoint requires a session except those marked AllowAnonymous.
            .SetFallbackPolicy(WithActiveSession(new AuthorizationPolicyBuilder()).Build())
            .AddPolicy(Policies.SignedIn, p => p
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser())
            .AddPolicy(Policies.Administrators, p => WithActiveSession(p)
                .RequireRole(Roles.SuperUser, Roles.Administracion))
            .AddPolicy(Policies.CapturesAddresses, p => WithActiveSession(p)
                .AddRequirements(new AnyScreenRequirement(
                    [Screens.Agendar, Screens.ParadasFijas, Screens.Rutas, Screens.Operacion, Screens.AppInvitado], AccessLevel.Edit)))
            .AddPolicy(Policies.SeesOperacion, p => WithActiveSession(p)
                .AddRequirements(new AnyScreenRequirement([Screens.PanelDelDia, Screens.Rutas, Screens.VistaDelChofer], AccessLevel.View)))
            .AddPolicy(Policies.AssignsRutaDia, p => WithActiveSession(p)
                .AddRequirements(new AnyScreenRequirement([Screens.PanelDelDia, Screens.Rutas], AccessLevel.Edit)))
            .AddPolicy(Policies.UsesAgenda, p => WithActiveSession(p)
                .AddRequirements(new AnyScreenRequirement([Screens.Agendar, Screens.AppInvitado], AccessLevel.View)))
            .AddPolicy(Policies.SchedulesAgenda, p => WithActiveSession(p)
                .AddRequirements(new AnyScreenRequirement([Screens.Agendar, Screens.AppInvitado], AccessLevel.Edit)));

        return services;
    }
}
