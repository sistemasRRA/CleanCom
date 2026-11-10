using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CleanCom.Infrastructure.Identity;

/// <summary>"Jwt" configuration section.</summary>
public class JwtOptions
{
    public string Issuer { get; set; } = "cleancom-api";
    public string Audience { get; set; } = "cleancom-clients";
    /// <summary>Signing key (HS256), at least 32 characters. Lives in user-secrets or an environment variable.</summary>
    public string Key { get; set; } = "";
    public int AccessMinutes { get; set; } = 30;
    public int RefreshDays { get; set; } = 14;

    public SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(Key));
}

/// <summary>
/// Claim names in the access token.
/// </summary>
public static class CleanComClaims
{
    public const string Subject = JwtRegisteredClaimNames.Sub;
    public const string UserName = "name";
    public const string FullName = "full_name";
    public const string Role = "role";
    /// <summary>Present while the user has not changed the initial or temporary password.</summary>
    public const string MustChangePassword = "must_change_password";
}

public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    /// <summary>
    /// Permissions are not in the token: the API reads them per request from IPermissionStore,
    /// so a change in Roles y permisos applies without signing in again.
    /// </summary>
    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user, string role, DateTimeOffset now)
    {
        var expiresAt = now.AddMinutes(_options.AccessMinutes);
        var claims = new List<Claim>
        {
            new(CleanComClaims.Subject, user.Id.ToString()),
            new(CleanComClaims.UserName, user.UserName ?? ""),
            new(CleanComClaims.FullName, user.FullName),
            new(CleanComClaims.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        if (user.MustChangePassword)
        {
            claims.Add(new Claim(CleanComClaims.MustChangePassword, "true"));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(_options.SigningKey(), SecurityAlgorithms.HmacSha256),
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    public (string Token, string Hash, DateTimeOffset ExpiresAt) CreateRefreshToken(DateTimeOffset now)
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return (token, Hash(token), now.AddDays(_options.RefreshDays));
    }

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
