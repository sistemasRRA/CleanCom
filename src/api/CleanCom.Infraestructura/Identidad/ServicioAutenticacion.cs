using CleanCom.Aplicacion.Acceso;
using CleanCom.Aplicacion.Comun;
using CleanCom.Dominio.Acceso;
using CleanCom.Dominio.Comun;
using CleanCom.Dominio.Tiempo;
using CleanCom.Infraestructura.Persistencia;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Infraestructura.Identidad;

public class ServicioAutenticacion(
    UserManager<Usuario> usuarios,
    CleanComDbContext db,
    ServicioTokens tokens,
    IRelojNegocio reloj,
    IValidator<IniciarSesionRequest> validadorInicio,
    IValidator<CambiarContrasenaRequest> validadorCambio) : IServicioAutenticacion
{
    private const string CredencialesInvalidas = "Usuario o contraseña incorrectos.";
    private const string SesionExpirada = "La sesión expiró. Inicia sesión de nuevo.";

    public async Task<SesionDto> IniciarSesionAsync(IniciarSesionRequest request, CancellationToken ct)
    {
        await validadorInicio.ValidateAndThrowAsync(request, ct);
        var nombre = request.Usuario.Trim();
        var usuario = await usuarios.FindByNameAsync(nombre)
            ?? (nombre.Contains('@') ? await usuarios.FindByEmailAsync(nombre) : null);

        if (usuario is null || !usuario.Activo)
        {
            throw new ExcepcionNoAutorizado(CredencialesInvalidas);
        }
        if (await usuarios.IsLockedOutAsync(usuario))
        {
            throw new ExcepcionNoAutorizado("La cuenta está bloqueada por varios intentos fallidos. Intenta de nuevo en unos minutos.");
        }
        if (!await usuarios.CheckPasswordAsync(usuario, request.Contrasena))
        {
            await usuarios.AccessFailedAsync(usuario);
            throw new ExcepcionNoAutorizado(CredencialesInvalidas);
        }

        await usuarios.ResetAccessFailedCountAsync(usuario);
        usuario.UltimoAcceso = reloj.AhoraUtc;
        await usuarios.UpdateAsync(usuario);
        return await EmitirSesionAsync(usuario, ct);
    }

    public async Task<SesionDto> RenovarAsync(RenovarSesionRequest request, CancellationToken ct)
    {
        var ahora = reloj.AhoraUtc;
        var hash = ServicioTokens.Hash(request.TokenRenovacion ?? "");
        var registro = await db.TokensRenovacion.FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new ExcepcionNoAutorizado(SesionExpirada);

        if (registro.RevocadoEn is not null)
        {
            // Un token ya usado vuelve a llegar: puede ser robado. Se cierran todas las sesiones del usuario.
            await RevocarTodosAsync(registro.UsuarioId, ct);
            throw new ExcepcionNoAutorizado(SesionExpirada);
        }
        if (registro.ExpiraEn <= ahora)
        {
            throw new ExcepcionNoAutorizado(SesionExpirada);
        }

        registro.RevocadoEn = ahora;
        var usuario = await usuarios.FindByIdAsync(registro.UsuarioId.ToString());
        if (usuario is null || !usuario.Activo)
        {
            await db.SaveChangesAsync(ct);
            throw new ExcepcionNoAutorizado(SesionExpirada);
        }
        return await EmitirSesionAsync(usuario, ct);
    }

    public async Task CerrarSesionAsync(RenovarSesionRequest request, CancellationToken ct)
    {
        var hash = ServicioTokens.Hash(request.TokenRenovacion ?? "");
        var registro = await db.TokensRenovacion.FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevocadoEn == null, ct);
        if (registro is not null)
        {
            registro.RevocadoEn = reloj.AhoraUtc;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<SesionDto> CambiarContrasenaAsync(int usuarioId, CambiarContrasenaRequest request, CancellationToken ct)
    {
        await validadorCambio.ValidateAndThrowAsync(request, ct);
        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString())
            ?? throw new ExcepcionNoAutorizado(SesionExpirada);

        var resultado = await usuarios.ChangePasswordAsync(usuario, request.ContrasenaActual, request.ContrasenaNueva);
        ErroresIdentidad.LanzarSiFalla(resultado);

        usuario.DebeCambiarContrasena = false;
        await usuarios.UpdateAsync(usuario);
        await RevocarTodosAsync(usuario.Id, ct);
        return await EmitirSesionAsync(usuario, ct);
    }

    public async Task<UsuarioSesionDto> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct)
    {
        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString())
            ?? throw new ExcepcionNoEncontrado("El usuario no existe.");
        var (rol, permisos) = await RolYPermisosAsync(usuario, ct);
        return AUsuarioSesion(usuario, rol, permisos);
    }

    private async Task<SesionDto> EmitirSesionAsync(Usuario usuario, CancellationToken ct)
    {
        var ahora = reloj.AhoraUtc;
        var (rol, permisos) = await RolYPermisosAsync(usuario, ct);
        var (acceso, expira) = tokens.CrearTokenAcceso(usuario, rol, permisos, ahora);
        var (renovacion, hash, renovacionExpira) = tokens.CrearTokenRenovacion(ahora);

        db.TokensRenovacion.Add(new TokenRenovacion
        {
            UsuarioId = usuario.Id,
            TokenHash = hash,
            CreadoEn = ahora,
            ExpiraEn = renovacionExpira,
        });
        await db.SaveChangesAsync(ct);

        return new SesionDto(acceso, expira, renovacion, renovacionExpira, AUsuarioSesion(usuario, rol, permisos));
    }

    private async Task<(string Rol, IReadOnlyList<string> Permisos)> RolYPermisosAsync(Usuario usuario, CancellationToken ct)
    {
        var rol = (await usuarios.GetRolesAsync(usuario)).FirstOrDefault() ?? "";
        var permisos = await (
                from p in db.PermisosRol
                join r in db.Roles on p.RolId equals r.Id
                where r.Name == rol
                orderby p.Pantalla
                select new { p.Pantalla, p.Nivel })
            .ToListAsync(ct);
        return (rol, permisos.Select(p => Permisos.AClaim(p.Pantalla, p.Nivel)).ToList());
    }

    private async Task RevocarTodosAsync(int usuarioId, CancellationToken ct)
    {
        var ahora = reloj.AhoraUtc;
        await db.TokensRenovacion
            .Where(t => t.UsuarioId == usuarioId && t.RevocadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoEn, ahora), ct);
    }

    private static UsuarioSesionDto AUsuarioSesion(Usuario u, string rol, IReadOnlyList<string> permisos) =>
        new(u.Id, u.NombreCompleto, u.UserName ?? "", rol, u.DebeCambiarContrasena, permisos);
}

/// <summary>
/// Errores de Identity en español.
/// </summary>
public class DescriptorErroresIdentidad : IdentityErrorDescriber
{
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "La contraseña actual no es correcta." };
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"La contraseña debe tener al menos {length} caracteres." };
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = $"El usuario {userName} ya existe." };
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = $"El correo {email} ya está registrado." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = $"El usuario {userName} no es válido." };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = $"El correo {email} no es válido." };
}

internal static class ErroresIdentidad
{
    public static void LanzarSiFalla(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
        {
            throw new ExcepcionDominio(string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }
    }
}
