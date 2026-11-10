using CleanCom.Application.Common;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Geography;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Catalogs;

/// <summary>
/// Operación catalogs. Deletes are soft, except for non-working days and coverage, which are removed.
/// </summary>
public class CatalogService(ICleanComDbContext db, IServiceProvider validators)
{
    private Task Validate<T>(T request, CancellationToken ct) =>
        ((IValidator<T>)validators.GetService(typeof(IValidator<T>))!).ValidateAndThrowAsync(request, ct);

    // Rutas

    public async Task<IReadOnlyList<RutaDto>> ListRutasAsync(CancellationToken ct) =>
        await db.Rutas.AsNoTracking().OrderBy(r => r.Code).Select(r => ToRutaDto(r)).ToListAsync(ct);

    public async Task<RutaDto> CreateRutaAsync(SaveRutaRequest r, CancellationToken ct)
    {
        await Validate(r, ct);
        if (await db.Rutas.AnyAsync(x => x.Code == r.Code, ct))
        {
            throw new ConflictException($"Ya existe la ruta {r.Code}.");
        }
        var ruta = new Ruta();
        ApplyRuta(ruta, r);
        db.Rutas.Add(ruta);
        await db.SaveChangesAsync(ct);
        return ToRutaDto(ruta);
    }

    public async Task<RutaDto> UpdateRutaAsync(int id, SaveRutaRequest r, CancellationToken ct)
    {
        await Validate(r, ct);
        var ruta = await db.Rutas.FindAsync([id], ct) ?? throw new NotFoundException($"La ruta {id} no existe.");
        if (await db.Rutas.AnyAsync(x => x.Code == r.Code && x.Id != id, ct))
        {
            throw new ConflictException($"Ya existe la ruta {r.Code}.");
        }
        ApplyRuta(ruta, r);
        await db.SaveChangesAsync(ct);
        return ToRutaDto(ruta);
    }

    private static void ApplyRuta(Ruta ruta, SaveRutaRequest r)
    {
        ruta.Code = r.Code;
        ruta.Name = r.Name.Trim();
        ruta.Color = r.Color.ToUpperInvariant();
        ruta.CupoParadas = r.CupoParadas;
        ruta.RecoleccionGoal = r.RecoleccionGoal;
        ruta.MaxReturnTime = r.MaxReturnTime;
        ruta.MaxReturnTimeSaturday = r.MaxReturnTimeSaturday;
        ruta.Active = r.Active;
    }

    private static RutaDto ToRutaDto(Ruta r) =>
        new(r.Id, r.Code, r.Name, r.Color, r.CupoParadas, r.RecoleccionGoal, r.MaxReturnTime, r.MaxReturnTimeSaturday, r.Active);

    // Lugares

    public async Task<IReadOnlyList<LugarDto>> ListLugaresAsync(bool includeInactive, CancellationToken ct)
    {
        var lugares = await db.Lugares.AsNoTracking()
            .Where(l => includeInactive || l.Active)
            .OrderBy(l => l.Type).ThenBy(l => l.Name)
            .ToListAsync(ct);
        return lugares.Select(ToLugarDto).ToList();
    }

    public async Task<LugarDto> CreateLugarAsync(SaveLugarRequest r, CancellationToken ct)
    {
        await Validate(r, ct);
        var lugar = new Lugar();
        ApplyLugar(lugar, r);
        db.Lugares.Add(lugar);
        await db.SaveChangesAsync(ct);
        return ToLugarDto(lugar);
    }

    public async Task<LugarDto> UpdateLugarAsync(int id, SaveLugarRequest r, CancellationToken ct)
    {
        await Validate(r, ct);
        var lugar = await db.Lugares.FindAsync([id], ct) ?? throw new NotFoundException($"El lugar {id} no existe.");
        ApplyLugar(lugar, r);
        await db.SaveChangesAsync(ct);
        return ToLugarDto(lugar);
    }

    public async Task DeactivateLugarAsync(int id, CancellationToken ct)
    {
        var lugar = await db.Lugares.FindAsync([id], ct) ?? throw new NotFoundException($"El lugar {id} no existe.");
        lugar.Active = false;
        await db.SaveChangesAsync(ct);
    }

    private static void ApplyLugar(Lugar lugar, SaveLugarRequest r)
    {
        lugar.Name = r.Name.Trim();
        lugar.Type = r.Type;
        lugar.Address = r.Address.Trim();
        lugar.Location = r.Lat is { } lat && r.Lng is { } lng ? Geo.CreatePoint(lat, lng) : null;
        lugar.OpeningHours = string.IsNullOrWhiteSpace(r.OpeningHours) ? null : r.OpeningHours.Trim();
        lugar.Active = r.Active;
    }

    private static LugarDto ToLugarDto(Lugar l) =>
        new(l.Id, l.Name, l.Type, l.Address, l.Location?.Y, l.Location?.X, l.OpeningHours, l.Active);

    // Parada types

    public async Task<IReadOnlyList<ParadaTypeDto>> ListParadaTypesAsync(CancellationToken ct) =>
        await db.ParadaTypes.AsNoTracking().OrderBy(t => t.Name)
            .Select(t => new ParadaTypeDto(t.Id, t.Name, t.RequiredFields, t.Active)).ToListAsync(ct);

    public async Task<ParadaTypeDto> SaveParadaTypeAsync(int? id, SaveParadaTypeRequest r, CancellationToken ct)
    {
        await Validate(r, ct);
        var type = id is null ? new ParadaType() : await db.ParadaTypes.FindAsync([id], ct) ?? throw new NotFoundException($"El tipo de parada {id} no existe.");
        if (await db.ParadaTypes.AnyAsync(x => x.Name == r.Name.Trim() && x.Id != type.Id, ct))
        {
            throw new ConflictException($"Ya existe el tipo de parada {r.Name}.");
        }
        type.Name = r.Name.Trim();
        type.RequiredFields = string.IsNullOrWhiteSpace(r.RequiredFields) ? null : r.RequiredFields.Trim();
        type.Active = r.Active;
        if (id is null)
        {
            db.ParadaTypes.Add(type);
        }
        await db.SaveChangesAsync(ct);
        return new ParadaTypeDto(type.Id, type.Name, type.RequiredFields, type.Active);
    }

    // Líneas de servicio

    public async Task<IReadOnlyList<LineaServicioDto>> ListLineasServicioAsync(CancellationToken ct) =>
        await db.LineasServicio.AsNoTracking().OrderBy(l => l.Name)
            .Select(l => new LineaServicioDto(l.Id, l.Name, l.ProcessingDays, l.Active)).ToListAsync(ct);

    public async Task<LineaServicioDto> SaveLineaServicioAsync(int? id, SaveLineaServicioRequest r, CancellationToken ct)
    {
        await Validate(r, ct);
        var linea = id is null ? new LineaServicio() : await db.LineasServicio.FindAsync([id], ct) ?? throw new NotFoundException($"La línea de servicio {id} no existe.");
        if (await db.LineasServicio.AnyAsync(x => x.Name == r.Name.Trim() && x.Id != linea.Id, ct))
        {
            throw new ConflictException($"Ya existe la línea de servicio {r.Name}.");
        }
        linea.Name = r.Name.Trim();
        linea.ProcessingDays = r.ProcessingDays;
        linea.Active = r.Active;
        if (id is null)
        {
            db.LineasServicio.Add(linea);
        }
        await db.SaveChangesAsync(ct);
        return new LineaServicioDto(linea.Id, linea.Name, linea.ProcessingDays, linea.Active);
    }

    // Reasons

    public async Task<IReadOnlyList<ReasonDto>> ListReasonsAsync(ReasonType? type, CancellationToken ct) =>
        await db.Reasons.AsNoTracking()
            .Where(m => type == null || m.Type == type)
            .OrderBy(m => m.Type).ThenBy(m => m.Text)
            .Select(m => new ReasonDto(m.Id, m.Type, m.Text, m.Active)).ToListAsync(ct);

    public async Task<ReasonDto> SaveReasonAsync(int? id, SaveReasonRequest r, CancellationToken ct)
    {
        await Validate(r, ct);
        var reason = id is null ? new Reason() : await db.Reasons.FindAsync([id], ct) ?? throw new NotFoundException($"El motivo {id} no existe.");
        reason.Type = r.Type;
        reason.Text = r.Text.Trim();
        reason.Active = r.Active;
        if (id is null)
        {
            db.Reasons.Add(reason);
        }
        await db.SaveChangesAsync(ct);
        return new ReasonDto(reason.Id, reason.Type, reason.Text, reason.Active);
    }

    // Non-working days

    public async Task<IReadOnlyList<NonWorkingDayDto>> ListNonWorkingDaysAsync(DateOnly? from, DateOnly? to, CancellationToken ct) =>
        await db.NonWorkingDays.AsNoTracking()
            .Where(d => (from == null || d.Date >= from) && (to == null || d.Date <= to))
            .OrderBy(d => d.Date)
            .Select(d => new NonWorkingDayDto(d.Id, d.Date, d.Reason)).ToListAsync(ct);

    public async Task<NonWorkingDayDto> SaveNonWorkingDayAsync(int? id, SaveNonWorkingDayRequest r, CancellationToken ct)
    {
        await Validate(r, ct);
        var day = id is null ? new NonWorkingDay() : await db.NonWorkingDays.FindAsync([id], ct) ?? throw new NotFoundException($"El día inhábil {id} no existe.");
        if (await db.NonWorkingDays.AnyAsync(x => x.Date == r.Date && x.Id != day.Id, ct))
        {
            throw new ConflictException($"El {r.Date:yyyy-MM-dd} ya está marcado como inhábil.");
        }
        day.Date = r.Date;
        day.Reason = r.Reason.Trim();
        if (id is null)
        {
            db.NonWorkingDays.Add(day);
        }
        await db.SaveChangesAsync(ct);
        return new NonWorkingDayDto(day.Id, day.Date, day.Reason);
    }

    public async Task DeleteNonWorkingDayAsync(int id, CancellationToken ct)
    {
        var day = await db.NonWorkingDays.FindAsync([id], ct) ?? throw new NotFoundException($"El día inhábil {id} no existe.");
        db.NonWorkingDays.Remove(day);
        await db.SaveChangesAsync(ct);
    }

    // Coverage

    public async Task<IReadOnlyList<CoverageDto>> ListCoverageAsync(string? search, int? zonaId, CancellationToken ct)
    {
        var query = db.Coverage.AsNoTracking();
        if (zonaId is not null)
        {
            query = query.Where(c => c.ZonaId == zonaId);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(c => c.PostalCode.StartsWith(text) || c.Neighborhood.Contains(text) || c.Borough.Contains(text));
        }
        return await query
            .OrderBy(c => c.PostalCode).ThenBy(c => c.Neighborhood)
            .Select(c => new CoverageDto(c.Id, c.PostalCode, c.Neighborhood, c.Borough, c.ZonaId, c.Zona!.Code))
            .ToListAsync(ct);
    }

    public async Task<CoverageDto> SaveCoverageAsync(int? id, SaveCoverageRequest r, CancellationToken ct)
    {
        await Validate(r, ct);
        var zona = await db.Zonas.AsNoTracking().FirstOrDefaultAsync(z => z.Id == r.ZonaId, ct)
            ?? throw new NotFoundException($"La zona {r.ZonaId} no existe.");
        var coverage = id is null ? new PostalCodeCoverage() : await db.Coverage.FindAsync([id], ct) ?? throw new NotFoundException($"El registro de cobertura {id} no existe.");
        var neighborhood = r.Neighborhood.Trim();
        if (await db.Coverage.AnyAsync(x => x.PostalCode == r.PostalCode && x.Neighborhood == neighborhood && x.Id != coverage.Id, ct))
        {
            throw new ConflictException($"La colonia {neighborhood} ({r.PostalCode}) ya está en la cobertura.");
        }
        coverage.PostalCode = r.PostalCode;
        coverage.Neighborhood = neighborhood;
        coverage.Borough = r.Borough.Trim();
        coverage.ZonaId = r.ZonaId;
        if (id is null)
        {
            db.Coverage.Add(coverage);
        }
        await db.SaveChangesAsync(ct);
        return new CoverageDto(coverage.Id, coverage.PostalCode, coverage.Neighborhood, coverage.Borough, zona.Id, zona.Code);
    }

    public async Task DeleteCoverageAsync(int id, CancellationToken ct)
    {
        var coverage = await db.Coverage.FindAsync([id], ct) ?? throw new NotFoundException($"El registro de cobertura {id} no existe.");
        db.Coverage.Remove(coverage);
        await db.SaveChangesAsync(ct);
    }
}
