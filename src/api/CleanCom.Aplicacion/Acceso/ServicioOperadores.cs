using CleanCom.Aplicacion.Comun;
using CleanCom.Dominio.Personas;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Aplicacion.Acceso;

public class ServicioOperadores(ICleanComDbContext db, IValidator<GuardarOperadorRequest> validador)
{
    public async Task<IReadOnlyList<OperadorDto>> ListarAsync(bool incluirInactivos, CancellationToken ct) =>
        await db.Operadores.AsNoTracking()
            .Where(o => incluirInactivos || o.Activo)
            .OrderBy(o => o.Nombre)
            .Select(o => new OperadorDto(o.Id, o.Nombre, o.Telefono, o.RutaHabitualId, o.Activo, o.UsuarioId))
            .ToListAsync(ct);

    public async Task<OperadorDto> GuardarAsync(int? id, GuardarOperadorRequest r, CancellationToken ct)
    {
        await validador.ValidateAndThrowAsync(r, ct);
        if (r.RutaHabitualId is { } rutaId && !await db.Rutas.AnyAsync(x => x.Id == rutaId, ct))
        {
            throw new ExcepcionNoEncontrado($"La ruta {rutaId} no existe.");
        }

        var operador = id is null ? new Operador() : await db.Operadores.FindAsync([id], ct) ?? throw new ExcepcionNoEncontrado($"El operador {id} no existe.");
        operador.Nombre = r.Nombre.Trim();
        operador.Telefono = string.IsNullOrWhiteSpace(r.Telefono) ? null : r.Telefono.Trim();
        operador.RutaHabitualId = r.RutaHabitualId;
        operador.Activo = r.Activo;
        if (id is null)
        {
            db.Operadores.Add(operador);
        }
        await db.SaveChangesAsync(ct);
        return new OperadorDto(operador.Id, operador.Nombre, operador.Telefono, operador.RutaHabitualId, operador.Activo, operador.UsuarioId);
    }
}
