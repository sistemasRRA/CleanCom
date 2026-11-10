using CleanCom.Application.Common;
using CleanCom.Domain.People;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Access;

public class OperadorService(ICleanComDbContext db, IValidator<SaveOperadorRequest> validator)
{
    public async Task<IReadOnlyList<OperadorDto>> ListAsync(bool includeInactive, CancellationToken ct) =>
        await db.Operadores.AsNoTracking()
            .Where(o => includeInactive || o.Active)
            .OrderBy(o => o.Name)
            .Select(o => new OperadorDto(o.Id, o.Name, o.Phone, o.UsualRutaId, o.Active, o.UserId))
            .ToListAsync(ct);

    public async Task<OperadorDto> SaveAsync(int? id, SaveOperadorRequest r, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(r, ct);
        if (r.UsualRutaId is { } rutaId && !await db.Rutas.AnyAsync(x => x.Id == rutaId, ct))
        {
            throw new NotFoundException($"La ruta {rutaId} no existe.");
        }

        var operador = id is null ? new Operador() : await db.Operadores.FindAsync([id], ct) ?? throw new NotFoundException($"El operador {id} no existe.");
        operador.Name = r.Name.Trim();
        operador.Phone = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim();
        operador.UsualRutaId = r.UsualRutaId;
        operador.Active = r.Active;
        if (id is null)
        {
            db.Operadores.Add(operador);
        }
        await db.SaveChangesAsync(ct);
        return new OperadorDto(operador.Id, operador.Name, operador.Phone, operador.UsualRutaId, operador.Active, operador.UserId);
    }
}
