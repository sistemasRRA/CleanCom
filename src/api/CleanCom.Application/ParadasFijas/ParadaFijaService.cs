using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Common;
using CleanCom.Domain.Geography;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.ParadasFijas;

/// <summary>
/// Paradas y vueltas fijas (docs/pantallas/oficina/paradas-fijas.md) and their generation into the rutas
/// (docs/03-reglas-de-negocio.md, Paradas fijas).
/// </summary>
public class ParadaFijaService(
    ICleanComDbContext db,
    ISettingService settings,
    IBusinessClock clock,
    FijaCalendar calendar,
    IValidator<SaveParadaFijaRequest> validator,
    IValidator<SaveParadaFijaExceptionRequest> exceptionValidator)
{
    public async Task<IReadOnlyList<ParadaFijaDto>> ListAsync(bool includeInactive, ParadaCategory? category, CancellationToken ct)
    {
        var fijas = await Query()
            .Where(f => (includeInactive || f.Active) && (category == null || f.Category == category))
            .OrderBy(f => f.Ruta!.Code).ThenBy(f => f.Name)
            .ToListAsync(ct);
        return fijas.Select(ToDto).ToList();
    }

    public async Task<ParadaFijaDto> GetAsync(int id, CancellationToken ct) =>
        ToDto(await Query().FirstOrDefaultAsync(f => f.Id == id, ct) ?? throw new NotFoundException($"La parada fija {id} no existe."));

    /// <summary>Creates it and generates its paradas for the days ahead.</summary>
    public async Task<ParadaFijaDto> CreateAsync(SaveParadaFijaRequest r, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(r, ct);
        var fija = new ParadaFija();
        await ApplyAsync(fija, r, ct);
        db.ParadasFijas.Add(fija);
        await db.SaveChangesAsync(ct);
        await RegenerateAheadAsync(fija.Id, ct);
        return await GetAsync(fija.Id, ct);
    }

    /// <summary>
    /// Saves the changes and replaces its paradas from today on that have not started (rule 4). Deactivating or pausing
    /// it takes them out of the rutas.
    /// </summary>
    public async Task<ParadaFijaDto> UpdateAsync(int id, SaveParadaFijaRequest r, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(r, ct);
        var fija = await db.ParadasFijas.Include(f => f.Pasos).FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException($"La parada fija {id} no existe.");
        await ApplyAsync(fija, r, ct);
        await db.SaveChangesAsync(ct);
        await RegenerateAheadAsync(fija.Id, ct);
        return await GetAsync(fija.Id, ct);
    }

    /// <summary>Skips one date. Its paradas that day leave the ruta unless the chofer already started them.</summary>
    public async Task<ParadaFijaDto> AddExceptionAsync(int id, SaveParadaFijaExceptionRequest r, CancellationToken ct)
    {
        await exceptionValidator.ValidateAndThrowAsync(r, ct);
        if (!await db.ParadasFijas.AnyAsync(f => f.Id == id, ct))
        {
            throw new NotFoundException($"La parada fija {id} no existe.");
        }
        if (r.Date < clock.Today)
        {
            throw new DomainException("La fecha ya pasó.");
        }
        if (await db.ParadaFijaExceptions.AnyAsync(e => e.ParadaFijaId == id && e.Date == r.Date && e.Active, ct))
        {
            throw new ConflictException($"El {AgendaCalculator.Format(r.Date)} ya está marcado como excepción.");
        }

        db.ParadaFijaExceptions.Add(new ParadaFijaException
        {
            ParadaFijaId = id,
            Date = r.Date,
            Reason = string.IsNullOrWhiteSpace(r.Reason) ? null : r.Reason.Trim(),
        });
        await db.SaveChangesAsync(ct);
        await RegenerateDateAsync(id, r.Date, ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Removes an exception (baja lógica); within the days ahead, the paradas come back to the ruta.</summary>
    public async Task<ParadaFijaDto> RemoveExceptionAsync(int id, int exceptionId, CancellationToken ct)
    {
        var exception = await db.ParadaFijaExceptions.FirstOrDefaultAsync(e => e.Id == exceptionId && e.ParadaFijaId == id && e.Active, ct)
            ?? throw new NotFoundException("La excepción no existe.");
        exception.Active = false;
        await db.SaveChangesAsync(ct);
        await RegenerateDateAsync(id, exception.Date, ct);
        return await GetAsync(id, ct);
    }

    /// <summary>
    /// "La semana de un vistazo" from Monday to Saturday of the week of <paramref name="date"/> (this week when empty):
    /// paradas fijas per ruta and day and the places they take of the cupo.
    /// </summary>
    public async Task<FijasWeekDto> WeekAsync(DateOnly? date, CancellationToken ct)
    {
        var day = date ?? clock.Today;
        var from = day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
        var to = from.AddDays(5);

        var rutas = await db.Rutas.AsNoTracking().Where(r => r.Active).OrderBy(r => r.Code)
            .Select(r => new FijasWeekRutaDto(r.Id, r.Code, r.Name, r.Color)).ToListAsync(ct);
        var cupos = new Dictionary<int, int>();
        foreach (var ruta in rutas)
        {
            cupos[ruta.Id] = await settings.GetIntegerAsync(SettingKeys.RutaCupoParadas, ct, ruta.Id);
        }

        var fijas = (await calendar.LoadActiveAsync(null, ct)).OrderBy(f => f.Placement == ParadaPlacement.Primera ? 0 : f.Placement == ParadaPlacement.Ultima ? 2 : 1)
            .ThenBy(f => f.Turno).ThenBy(f => f.Name).ToList();
        var nonWorking = await db.NonWorkingDays.AsNoTracking()
            .Where(d => d.Active && d.Date >= from && d.Date <= to)
            .ToDictionaryAsync(d => d.Date, d => d.Reason, ct);
        var generated = (await db.Paradas.AsNoTracking()
            .Where(p => p.Active && p.ParadaFijaId != null && p.RutaDia!.Date >= from && p.RutaDia.Date <= to)
            .Select(p => new { FijaId = p.ParadaFijaId!.Value, p.RutaDia!.Date })
            .Distinct()
            .ToListAsync(ct)).Select(x => (x.FijaId, x.Date)).ToHashSet();

        var nonWorkingSet = nonWorking.Keys.ToHashSet();
        var days = Enumerable.Range(0, 6).Select(i =>
        {
            var d = from.AddDays(i);
            return new FijasWeekDayDto(d, AgendaCalculator.Format(d), nonWorking.GetValueOrDefault(d), rutas.Select(ruta =>
            {
                var items = fijas.Where(f => f.RutaId == ruta.Id && f.AppliesOn(d, nonWorkingSet))
                    .Select(f => new FijasWeekItemDto(f.Id, f.Name, f.Category, f.Kind, f.Turno, f.Placement, f.ActivePasos.Count, generated.Contains((f.Id, d))))
                    .ToList();
                return new FijasWeekCellDto(ruta.Id, cupos[ruta.Id], items.Sum(x => x.Pasos), items);
            }).ToList());
        }).ToList();

        return new FijasWeekDto(from, to, rutas, days);
    }

    /// <summary>
    /// Nightly generation: the paradas fijas from today to <c>Fijas.DiasAnticipacion</c> days ahead that do not exist yet.
    /// Running it again creates nothing new.
    /// </summary>
    public async Task<GenerateFijasResultDto> GenerateAheadAsync(CancellationToken ct)
    {
        var from = clock.Today;
        var to = from.AddDays(await settings.GetIntegerAsync(SettingKeys.FixedDaysAhead, ct));
        var fijas = await calendar.LoadActiveAsync(null, ct);
        var (created, replaced, kept) = await GenerateAsync(fijas, from, to, replace: false, ct);
        return Result(from, to, created, replaced, kept);
    }

    /// <summary>"Agregar las fijas del…": replaces the paradas fijas of one day that have not started (rule 4).</summary>
    public async Task<GenerateFijasResultDto> RegenerateDayAsync(DateOnly date, CancellationToken ct)
    {
        if (date < clock.Today)
        {
            throw new DomainException("No se pueden regenerar días que ya pasaron.");
        }
        // Inactive ones too: their pending paradas leave the ruta.
        var fijas = await db.ParadasFijas.AsNoTracking()
            .Include(f => f.Pasos.Where(p => p.Active))
            .Include(f => f.Exceptions.Where(e => e.Active))
            .ToListAsync(ct);
        var (created, replaced, kept) = await GenerateAsync(fijas, date, date, replace: true, ct);
        return Result(date, date, created, replaced, kept);
    }

    private async Task RegenerateAheadAsync(int fijaId, CancellationToken ct)
    {
        var from = clock.Today;
        var to = from.AddDays(await settings.GetIntegerAsync(SettingKeys.FixedDaysAhead, ct));
        await GenerateAsync([await LoadForGenerationAsync(fijaId, ct)], from, to, replace: true, ct);
    }

    /// <summary>One date of one parada fija, only within the days that are generated ahead.</summary>
    private async Task RegenerateDateAsync(int fijaId, DateOnly date, CancellationToken ct)
    {
        var horizon = clock.Today.AddDays(await settings.GetIntegerAsync(SettingKeys.FixedDaysAhead, ct));
        if (date >= clock.Today && date <= horizon)
        {
            await GenerateAsync([await LoadForGenerationAsync(fijaId, ct)], date, date, replace: true, ct);
        }
    }

    private async Task<ParadaFija> LoadForGenerationAsync(int id, CancellationToken ct) =>
        await db.ParadasFijas.AsNoTracking()
            .Include(f => f.Pasos.Where(p => p.Active))
            .Include(f => f.Exceptions.Where(e => e.Active))
            .FirstAsync(f => f.Id == id, ct);

    /// <summary>
    /// Creates the paradas of <paramref name="fijas"/> in the range. With <paramref name="replace"/>, the ones already
    /// generated are deactivated and created again, except when the chofer already started one of them.
    /// </summary>
    private async Task<(int Created, int Replaced, int Kept)> GenerateAsync(
        IReadOnlyList<ParadaFija> fijas, DateOnly from, DateOnly to, bool replace, CancellationToken ct)
    {
        if (fijas.Count == 0)
        {
            return (0, 0, 0);
        }
        var fijaIds = fijas.Select(f => f.Id).ToList();
        var rutaIds = fijas.Select(f => f.RutaId).Distinct().ToList();
        var nonWorking = await calendar.NonWorkingDaysAsync(from, to, ct);
        var existing = await db.Paradas
            .Include(p => p.RutaDia)
            .Include(p => p.Viaje)
            .Where(p => p.ParadaFijaId != null && fijaIds.Contains(p.ParadaFijaId.Value) && p.RutaDia!.Date >= from && p.RutaDia.Date <= to)
            .ToListAsync(ct);
        var rutaDias = await db.RutaDias
            .Where(d => d.Date >= from && d.Date <= to && rutaIds.Contains(d.RutaId))
            .ToDictionaryAsync(d => (d.RutaId, d.Date), ct);

        int created = 0, replaced = 0, kept = 0;
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var fija in fijas)
            {
                var mine = existing.Where(p => p.ParadaFijaId == fija.Id && p.RutaDia!.Date == date).ToList();
                if (mine.Count > 0)
                {
                    if (!replace)
                    {
                        continue;
                    }
                    var active = mine.Where(p => p.Active).ToList();
                    if (active.Any(p => p.State != ParadaState.Pendiente))
                    {
                        kept++;
                        continue;
                    }
                    foreach (var parada in active)
                    {
                        parada.Active = false;
                        parada.Viaje?.Active = false;
                    }
                    if (active.Count > 0)
                    {
                        replaced++;
                    }
                }

                if (!fija.AppliesOn(date, nonWorking))
                {
                    continue;
                }
                if (!rutaDias.TryGetValue((fija.RutaId, date), out var rutaDia))
                {
                    rutaDia = new RutaDia { RutaId = fija.RutaId, Date = date };
                    db.RutaDias.Add(rutaDia);
                    rutaDias[(fija.RutaId, date)] = rutaDia;
                }
                db.Paradas.AddRange(fija.BuildParadas(rutaDia));
                created++;
            }
        }

        await db.SaveChangesAsync(ct);
        return (created, replaced, kept);
    }

    private static GenerateFijasResultDto Result(DateOnly from, DateOnly to, int created, int replaced, int kept)
    {
        var range = from == to ? $"el {AgendaCalculator.Format(from)}" : $"del {AgendaCalculator.Format(from)} al {AgendaCalculator.Format(to)}";
        var message = created == 0 && replaced == 0 && kept == 0
            ? $"No hubo paradas fijas que generar {range}."
            : $"Paradas fijas {range}: {created} generadas"
              + (replaced > 0 ? $", {replaced} reemplazadas" : "")
              + (kept > 0 ? $", {kept} sin cambio porque el chofer ya las inició" : "") + ".";
        return new GenerateFijasResultDto(from, to, created, replaced, kept, message);
    }

    private async Task ApplyAsync(ParadaFija fija, SaveParadaFijaRequest r, CancellationToken ct)
    {
        var name = r.Name.Trim();
        if (!await db.Rutas.AnyAsync(x => x.Id == r.RutaId && x.Active, ct))
        {
            throw new NotFoundException($"La ruta {r.RutaId} no existe o está inactiva.");
        }
        if (r.Active && await db.ParadasFijas.AnyAsync(x => x.Name == name && x.Active && x.Id != fija.Id, ct))
        {
            throw new ConflictException($"Ya existe la parada fija {name}.");
        }
        var lugarIds = r.Pasos.Where(p => p.LugarId is not null).Select(p => p.LugarId!.Value).Distinct().ToList();
        if (lugarIds.Count > 0 && await db.Lugares.CountAsync(l => lugarIds.Contains(l.Id) && l.Active, ct) != lugarIds.Count)
        {
            throw new DomainException("Algún lugar no existe o está inactivo.");
        }

        fija.Name = name;
        fija.Category = r.Category;
        fija.Kind = r.Kind;
        fija.RutaId = r.RutaId;
        fija.Turno = r.Turno;
        fija.Placement = r.Placement;
        fija.Days = r.Days.Distinct().Order().ToList();
        fija.Carried = Clean(r.Carried);
        fija.SiteContact = Clean(r.SiteContact);
        fija.SitePhone = Clean(r.SitePhone);
        fija.GarmentsApprox = r.GarmentsApprox;
        fija.DriverNotes = Clean(r.DriverNotes);
        fija.ValidFrom = r.ValidFrom;
        fija.ValidUntil = r.ValidUntil;
        fija.Paused = r.Paused;
        fija.Active = r.Active;

        // Pasos are rewritten in place by position; the ones left over are deactivated, never deleted.
        var current = fija.Pasos.Where(p => p.Active).OrderBy(p => p.Order).ToList();
        for (var i = 0; i < r.Pasos.Count; i++)
        {
            var paso = i < current.Count ? current[i] : new ParadaFijaPaso();
            var source = r.Pasos[i];
            paso.Order = i + 1;
            paso.LugarId = source.LugarId;
            paso.Address = source.LugarId is null ? Clean(source.Address) : null;
            paso.Location = source.LugarId is null ? Location(source, i + 1) : null;
            paso.Activity = Clean(source.Activity);
            paso.Notes = Clean(source.Notes);
            if (i >= current.Count)
            {
                fija.Pasos.Add(paso);
            }
        }
        foreach (var leftover in current.Skip(r.Pasos.Count))
        {
            leftover.Active = false;
            // Frees the position for the filtered unique index.
            leftover.Order = -leftover.Id;
        }
    }

    /// <summary>Coordinates as given, or read from the Google Maps link.</summary>
    private static NetTopologySuite.Geometries.Point? Location(SaveParadaFijaPasoRequest paso, int number)
    {
        if (paso.Lat is { } lat && paso.Lng is { } lng)
        {
            return Geo.CreatePoint(lat, lng);
        }
        if (string.IsNullOrWhiteSpace(paso.MapsLink))
        {
            return null;
        }
        return Geo.TryParseCoordinates(paso.MapsLink, out var a, out var b)
            ? Geo.CreatePoint(a, b)
            : throw new DomainException($"Paso {number}: no encontré coordenadas en el link. Si es un link corto (maps.app.goo.gl), ábrelo y copia el link completo del navegador o las coordenadas (19.36, -99.19).");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IQueryable<ParadaFija> Query() =>
        db.ParadasFijas.AsNoTracking()
            .Include(f => f.Ruta)
            .Include(f => f.Pasos.Where(p => p.Active)).ThenInclude(p => p.Lugar)
            .Include(f => f.Exceptions.Where(e => e.Active));

    private ParadaFijaDto ToDto(ParadaFija f) => new(
        f.Id,
        f.Name,
        f.Category,
        f.Kind,
        f.RutaId,
        f.Ruta?.Code ?? "",
        f.Ruta?.Color ?? "",
        f.Turno,
        f.Placement,
        f.Days,
        f.Carried,
        f.SiteContact,
        f.SitePhone,
        f.GarmentsApprox,
        f.DriverNotes,
        f.ValidFrom,
        f.ValidUntil,
        f.Paused,
        f.Active,
        f.IsVuelta,
        f.ActivePasos.Select(p => new ParadaFijaPasoDto(
            p.Order, p.LugarId, p.Lugar?.Name, p.Lugar?.Address ?? p.Address,
            (p.Lugar?.Location ?? p.Location)?.Y, (p.Lugar?.Location ?? p.Location)?.X, p.Activity, p.Notes)).ToList(),
        f.Exceptions.Where(e => e.Active && e.Date >= clock.Today).OrderBy(e => e.Date)
            .Select(e => new ParadaFijaExceptionDto(e.Id, e.Date, AgendaCalculator.Format(e.Date), e.Reason)).ToList());
}
