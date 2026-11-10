using System.Security.Claims;
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

    public const string ScreenPrefix = "Screen:";

    public static string Screen(string screen, AccessLevel level) => $"{ScreenPrefix}{screen}:{level}";
}

/// <summary>
/// "Screen:{screen}:{level}" policies built on the fly from the permissions in the token.
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

        var screen = parts[0];
        return Security.WithActiveSession(new AuthorizationPolicyBuilder())
            .RequireAssertion(ctx => Permissions.Allows(
                ctx.User.FindAll(CleanComClaims.Permission).Select(c => c.Value), screen, level))
            .Build();
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
            });

        services.AddSingleton<IAuthorizationPolicyProvider, ScreenPolicyProvider>();
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(WithActiveSession(new AuthorizationPolicyBuilder()).Build())
            // Every endpoint requires a session except those marked AllowAnonymous.
            .SetFallbackPolicy(WithActiveSession(new AuthorizationPolicyBuilder()).Build())
            .AddPolicy(Policies.SignedIn, p => p
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());

        return services;
    }
}
