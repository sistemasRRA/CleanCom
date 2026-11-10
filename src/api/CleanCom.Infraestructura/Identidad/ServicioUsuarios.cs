using CleanCom.Aplicacion.Acceso;
using CleanCom.Aplicacion.Comun;
using CleanCom.Dominio.Acceso;
using CleanCom.Dominio.Comun;
using CleanCom.Dominio.Personas;
using CleanCom.Dominio.Tiempo;
using CleanCom.Infraestructura.Persistencia;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Infraestructura.Identidad;

public class ServicioUsuarios(
    UserManager<Usuario> usuarios,
    CleanComDbContext db,
    IRelojNegocio reloj,
    IValidator<CrearUsuarioRequest> validadorCrear,
    IValidator<ActualizarUsuarioRequest> validadorActualizar,
    IValidator<RestablecerContrasenaRequest> validadorRestablecer) : IServicioUsuarios
{
    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(string? rol, CancellationToken ct) =>
        await Consulta(id: null, rol).ToListAsync(ct);

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioRequest r, CancellationToken ct)
    {
        await validadorCrear.ValidateAndThrowAsync(r, ct);
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var usuario = new Usuario
        {
            UserName = r.Usuario.Trim(),
            Email = Limpio(r.Correo),
            PhoneNumber = Limpio(r.Telefono),
            NombreCompleto = r.NombreCompleto.Trim(),
            DebeCambiarContrasena = true,
            CreadoEn = reloj.AhoraUtc,
        };
        ErroresIdentidad.LanzarSiFalla(await usuarios.CreateAsync(usuario, r.ContrasenaInicial));
        ErroresIdentidad.LanzarSiFalla(await usuarios.AddToRoleAsync(usuario, r.Rol));

        if (r.OperadorId is { } operadorId)
        {
            await LigarOperadorAsync(usuario.Id, operadorId, ct);
        }
        else if (r.CrearOperador)
        {
            if (r.RutaHabitualId is { } rutaId && !await db.Rutas.AnyAsync(x => x.Id == rutaId, ct))
            {
                throw new ExcepcionNoEncontrado($"La ruta {rutaId} no existe.");
            }
            db.Operadores.Add(new Operador
            {
                Nombre = usuario.NombreCompleto,
                Telefono = usuario.PhoneNumber,
                RutaHabitualId = r.RutaHabitualId,
                UsuarioId = usuario.Id,
            });
        }

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return await ObtenerAsync(usuario.Id, ct);
    }

    public async Task<UsuarioDto> ActualizarAsync(int id, ActualizarUsuarioRequest r, CancellationToken ct)
    {
        await validadorActualizar.ValidateAndThrowAsync(r, ct);
        var usuario = await usuarios.FindByIdAsync(id.ToString()) ?? throw new ExcepcionNoEncontrado($"El usuario {id} no existe.");
        var rolActual = (await usuarios.GetRolesAsync(usuario)).FirstOrDefault();

        var dejaDeSerAdmin = rolActual == Roles.Administracion && (r.Rol != Roles.Administracion || !r.Activo);
        if (dejaDeSerAdmin && await AdministradoresActivosAsync(ct) <= 1)
        {
            throw new ExcepcionDominio("Debe quedar al menos un usuario de Administración activo.");
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        usuario.NombreCompleto = r.NombreCompleto.Trim();
        usuario.Email = Limpio(r.Correo);
        usuario.PhoneNumber = Limpio(r.Telefono);
        usuario.Activo = r.Activo;
        ErroresIdentidad.LanzarSiFalla(await usuarios.UpdateAsync(usuario));

        if (rolActual != r.Rol)
        {
            if (rolActual is not null)
            {
                ErroresIdentidad.LanzarSiFalla(await usuarios.RemoveFromRoleAsync(usuario, rolActual));
            }
            ErroresIdentidad.LanzarSiFalla(await usuarios.AddToRoleAsync(usuario, r.Rol));
        }

        var ligado = await db.Operadores.FirstOrDefaultAsync(o => o.UsuarioId == id, ct);
        if (ligado is not null && ligado.Id != r.OperadorId)
        {
            ligado.UsuarioId = null;
            await db.SaveChangesAsync(ct);
        }
        if (r.OperadorId is { } operadorId && ligado?.Id != operadorId)
        {
            await LigarOperadorAsync(id, operadorId, ct);
        }

        if (!r.Activo || rolActual != r.Rol)
        {
            await RevocarSesionesAsync(id, ct);
        }

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    /// <summary>
    /// Oficina asigna una contraseña temporal; el usuario la cambia al entrar.
    /// </summary>
    public async Task RestablecerContrasenaAsync(int id, RestablecerContrasenaRequest r, CancellationToken ct)
    {
        await validadorRestablecer.ValidateAndThrowAsync(r, ct);
        var usuario = await usuarios.FindByIdAsync(id.ToString()) ?? throw new ExcepcionNoEncontrado($"El usuario {id} no existe.");

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        ErroresIdentidad.LanzarSiFalla(await usuarios.RemovePasswordAsync(usuario));
        ErroresIdentidad.LanzarSiFalla(await usuarios.AddPasswordAsync(usuario, r.ContrasenaTemporal));
        usuario.DebeCambiarContrasena = true;
        ErroresIdentidad.LanzarSiFalla(await usuarios.UpdateAsync(usuario));
        await usuarios.SetLockoutEndDateAsync(usuario, null);
        await usuarios.ResetAccessFailedCountAsync(usuario);
        await RevocarSesionesAsync(id, ct);
        await transaccion.CommitAsync(ct);
    }

    private async Task LigarOperadorAsync(int usuarioId, int operadorId, CancellationToken ct)
    {
        var operador = await db.Operadores.FindAsync([operadorId], ct) ?? throw new ExcepcionNoEncontrado($"El operador {operadorId} no existe.");
        if (operador.UsuarioId is not null && operador.UsuarioId != usuarioId)
        {
            throw new ExcepcionConflicto($"El operador {operador.Nombre} ya tiene otro usuario.");
        }
        operador.UsuarioId = usuarioId;
    }

    private Task<int> AdministradoresActivosAsync(CancellationToken ct) =>
        (from u in db.Users
         join ur in db.UserRoles on u.Id equals ur.UserId
         where ur.RoleId == Roles.IdAdministracion && u.Activo
         select u.Id).CountAsync(ct);

    private Task RevocarSesionesAsync(int usuarioId, CancellationToken ct)
    {
        var ahora = reloj.AhoraUtc;
        return db.TokensRenovacion
            .Where(t => t.UsuarioId == usuarioId && t.RevocadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoEn, ahora), ct);
    }

    private async Task<UsuarioDto> ObtenerAsync(int id, CancellationToken ct) =>
        await Consulta(id, rol: null).FirstAsync(ct);

    /// <summary>Los filtros van antes de proyectar al DTO para que EF los traduzca a SQL.</summary>
    private IQueryable<UsuarioDto> Consulta(int? id, string? rol) =>
        from u in db.Users.AsNoTracking()
        join ur in db.UserRoles on u.Id equals ur.UserId
        join r in db.Roles on ur.RoleId equals r.Id
        join o in db.Operadores on (int?)u.Id equals o.UsuarioId into operadores
        from o in operadores.DefaultIfEmpty()
        where (id == null || u.Id == id) && (rol == null || r.Name == rol)
        orderby u.NombreCompleto
        select new UsuarioDto(
            u.Id,
            u.NombreCompleto,
            u.UserName!,
            u.Email,
            u.PhoneNumber,
            r.Name!,
            u.Activo,
            u.DebeCambiarContrasena,
            u.UltimoAcceso,
            o == null ? null : o.Id,
            o == null ? null : o.Nombre);

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
