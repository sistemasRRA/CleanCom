using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CleanCom.Dominio.Acceso;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CleanCom.Infraestructura.Identidad;

public class OpcionesJwt
{
    public string Emisor { get; set; } = "cleancom-api";
    public string Audiencia { get; set; } = "cleancom-clientes";
    /// <summary>Llave de firma (HS256), al menos 32 caracteres. Va en user-secrets o variable de entorno.</summary>
    public string Llave { get; set; } = "";
    public int MinutosAcceso { get; set; } = 30;
    public int DiasRenovacion { get; set; } = 14;

    public SymmetricSecurityKey LlaveFirma() => new(Encoding.UTF8.GetBytes(Llave));
}

/// <summary>
/// Nombres de los claims del token de acceso.
/// </summary>
public static class ClaimsCleanCom
{
    public const string Sujeto = JwtRegisteredClaimNames.Sub;
    public const string Usuario = "name";
    public const string Nombre = "nombre";
    public const string Rol = "role";
    public const string Permiso = Permisos.TipoClaim;
    /// <summary>Presente mientras el usuario no cambie su contraseña inicial o temporal.</summary>
    public const string DebeCambiarContrasena = "debe_cambiar";
}

public class ServicioTokens(IOptions<OpcionesJwt> opciones)
{
    private readonly OpcionesJwt _opciones = opciones.Value;

    public (string Token, DateTimeOffset ExpiraEn) CrearTokenAcceso(Usuario usuario, string rol, IEnumerable<string> permisos, DateTimeOffset ahora)
    {
        var expira = ahora.AddMinutes(_opciones.MinutosAcceso);
        var claims = new List<Claim>
        {
            new(ClaimsCleanCom.Sujeto, usuario.Id.ToString()),
            new(ClaimsCleanCom.Usuario, usuario.UserName ?? ""),
            new(ClaimsCleanCom.Nombre, usuario.NombreCompleto),
            new(ClaimsCleanCom.Rol, rol),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        claims.AddRange(permisos.Select(p => new Claim(ClaimsCleanCom.Permiso, p)));
        if (usuario.DebeCambiarContrasena)
        {
            claims.Add(new Claim(ClaimsCleanCom.DebeCambiarContrasena, "true"));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _opciones.Emisor,
            Audience = _opciones.Audiencia,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = expira.UtcDateTime,
            SigningCredentials = new SigningCredentials(_opciones.LlaveFirma(), SecurityAlgorithms.HmacSha256),
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expira);
    }

    public (string Token, string Hash, DateTimeOffset ExpiraEn) CrearTokenRenovacion(DateTimeOffset ahora)
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return (token, Hash(token), ahora.AddDays(_opciones.DiasRenovacion));
    }

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
