using CleanCom.Application.Common;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Geography;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Catalogs;

public class ZonaService(
    ICleanComDbContext db,
    IValidator<SaveZonaRequest> zonaValidator,
    IValidator<VisitDaysRequest> daysValidator)
{
    public async Task<IReadOnlyList<ZonaDto>> ListAsync(int? rutaId, bool includeInactive, CancellationToken ct)
    {
        var zonas = await Query()
            .Where(z => (rutaId == null || z.RutaId == rutaId) && (includeInactive || z.Active))
            .OrderBy(z => z.Ruta!.Code).ThenBy(z => z.Code)
            .ToListAsync(ct);
        return zonas.Select(ToDto).ToList();
    }

    public async Task<ZonaDto> GetAsync(int id, CancellationToken ct) => ToDto(await Load(id, ct));

    public async Task<ZonaDto> CreateAsync(SaveZonaRequest r, CancellationToken ct)
    {
        await zonaValidator.ValidateAndThrowAsync(r, ct);
        var zona = new Zona();
        await Apply(zona, r, ct);
        db.Zonas.Add(zona);
        await db.SaveChangesAsync(ct);
        return await GetAsync(zona.Id, ct);
    }

    public async Task<ZonaDto> UpdateAsync(int id, SaveZonaRequest r, CancellationToken ct)
    {
        await zonaValidator.ValidateAndThrowAsync(r, ct);
        var zona = await db.Zonas.FindAsync([id], ct) ?? throw new NotFoundException($"La zona {id} no existe.");
        if (r.ZonaNucleoId is not null && !zona.IsExtension
            && await db.Zonas.AnyAsync(z => z.ZonaNucleoId == id, ct))
        {
            throw new DomainException($"La zona {zona.Code} es núcleo de otras zonas EXT; no puede volverse EXT.");
        }
        await Apply(zona, r, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct)
    {
        var zona = await db.Zonas.FindAsync([id], ct) ?? throw new NotFoundException($"La zona {id} no existe.");
        zona.Active = false;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Replaces the visit days. EXT zonas have no days of their own: they follow their zona núcleo.
    /// </summary>
    public async Task<ZonaDto> ChangeVisitDaysAsync(int id, VisitDaysRequest r, CancellationToken ct)
    {
        await daysValidator.ValidateAndThrowAsync(r, ct);
        var zona = await db.Zonas.Include(z => z.VisitDays).FirstOrDefaultAsync(z => z.Id == id, ct)
            ?? throw new NotFoundException($"La zona {id} no existe.");
        if (zona.IsExtension)
        {
            throw new DomainException($"La zona {zona.Code} es EXT y sigue los días de su zona núcleo; cambia los días del núcleo.");
        }

        var newDays = r.Days.Distinct().ToHashSet();
        db.ZonaVisitDays.RemoveRange(zona.VisitDays.Where(d => !newDays.Contains(d.DayOfWeek)));
        foreach (var day in newDays.Where(n => zona.VisitDays.All(d => d.DayOfWeek != n)))
        {
            zona.VisitDays.Add(new ZonaVisitDay { DayOfWeek = day });
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    private async Task Apply(Zona zona, SaveZonaRequest r, CancellationToken ct)
    {
        if (!await db.Rutas.AnyAsync(x => x.Id == r.RutaId, ct))
        {
            throw new NotFoundException($"La ruta {r.RutaId} no existe.");
        }
        if (await db.Zonas.AnyAsync(x => x.Code == r.Code && x.Id != zona.Id, ct))
        {
            throw new ConflictException($"Ya existe la zona {r.Code}.");
        }

        zona.Code = r.Code;
        zona.RutaId = r.RutaId;
        zona.Active = r.Active;
        zona.Polygon = r.Polygon is { Count: > 0 } ? Geo.CreatePolygon(r.Polygon) : null;

        if (r.ZonaNucleoId is { } nucleoId)
        {
            var nucleo = await db.Zonas.AsNoTracking().FirstOrDefaultAsync(z => z.Id == nucleoId, ct)
                ?? throw new NotFoundException($"La zona núcleo {nucleoId} no existe.");
            zona.ValidateNucleo(nucleo);
        }
        zona.ZonaNucleoId = r.ZonaNucleoId;
    }

    private IQueryable<Zona> Query() =>
        db.Zonas.AsNoTracking()
            .Include(z => z.Ruta)
            .Include(z => z.VisitDays)
            .Include(z => z.ZonaNucleo).ThenInclude(n => n!.VisitDays);

    private async Task<Zona> Load(int id, CancellationToken ct) =>
        await Query().FirstOrDefaultAsync(z => z.Id == id, ct) ?? throw new NotFoundException($"La zona {id} no existe.");

    private static ZonaDto ToDto(Zona z) => new(
        z.Id,
        z.Code,
        z.RutaId,
        z.Ruta?.Code ?? "",
        z.ZonaNucleoId,
        z.ZonaNucleo?.Code,
        z.Active,
        z.VisitDays.Select(d => d.DayOfWeek).Order().ToList(),
        z.EffectiveVisitDays(),
        z.Polygon is null ? null : Geo.ToVertices(z.Polygon));
}
