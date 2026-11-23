using CleanCom.Application.Chofer;
using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Geography;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Tracking;

/// <summary>
/// The chofer's GPS (docs/03-reglas-de-negocio.md, GPS del chofer): the phone sends readings during the shift, the last
/// one goes to the office's live map and a reduced history keeps the recorrido.
/// </summary>
public class LocationService(
    ICleanComDbContext db,
    IBusinessClock clock,
    ISettingService settings,
    ChoferService chofer,
    LiveLocations live,
    IOperacionNotifier notifier,
    IValidator<ChoferLocationsRequest> validator)
{
    /// <summary>
    /// Readings of the signed-in chofer for today's ruta. Without a ruta today they are not saved: the chofer is only
    /// followed during the shift.
    /// </summary>
    public async Task<LocationResultDto> RecordAsync(ChoferLocationsRequest r, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(r, ct);
        var today = await chofer.TodayAsync(ct);
        if (today is not { RutaDia: { } rutaDia })
        {
            return new LocationResultDto(false, "No tienes ruta hoy; tu ubicación no se guarda.");
        }

        var now = clock.UtcNow;
        var tolerance = await settings.GetIntegerAsync(SettingKeys.GpsToleranceMeters, ct);
        var readings = LocationTrail.Usable(
            r.Readings.Select(x => new LocationReading(x.Lat, x.Lng, x.At ?? now,
                x.AccuracyMeters is { } a ? (int)Math.Round(a) : null)),
            BusinessTime.ToUtc(rutaDia.Date, TimeOnly.MinValue), now, tolerance);
        if (readings.Count == 0)
        {
            return new LocationResultDto(false, "Las ubicaciones no son de hoy o son poco precisas.");
        }

        var lastKept = await db.ChoferLocations.AsNoTracking()
            .Where(u => u.RutaDiaId == rutaDia.Id)
            .OrderByDescending(u => u.RecordedAt)
            .Select(u => new { u.Location, u.RecordedAt, u.AccuracyMeters })
            .FirstOrDefaultAsync(ct);
        var keep = LocationTrail.ToKeep(
            lastKept is null ? null : new LocationReading(lastKept.Location.Y, lastKept.Location.X, lastKept.RecordedAt, lastKept.AccuracyMeters),
            readings,
            await settings.GetIntegerAsync(SettingKeys.GpsHistorySeconds, ct),
            await settings.GetIntegerAsync(SettingKeys.GpsHistoryMeters, ct));
        foreach (var k in keep)
        {
            db.ChoferLocations.Add(new ChoferLocation
            {
                RutaDiaId = rutaDia.Id,
                Location = Geo.CreatePoint(k.Lat, k.Lng),
                RecordedAt = k.At,
                AccuracyMeters = k.AccuracyMeters,
            });
        }
        if (keep.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        var current = live.Get(rutaDia.Id);
        var shown = current is null ? (LocationReading?)null : new LocationReading(current.Lat, current.Lng, current.At, current.AccuracyMeters);
        if (LocationTrail.Latest(shown, readings) is { } latest && latest != shown)
        {
            var noSignal = await settings.GetIntegerAsync(SettingKeys.GpsNoSignalMinutes, ct);
            var location = new LiveLocationDto(rutaDia.Id, rutaDia.Date, rutaDia.RutaId, rutaDia.Ruta!.Code, rutaDia.Ruta.Color,
                today.OperadorName, latest.Lat, latest.Lng, latest.At, latest.AccuracyMeters,
                SignalLostAt: PanelRules.SignalLostAt(latest.At, noSignal));
            live.Set(location);
            await notifier.LocationAsync(location, ct);
        }
        return new LocationResultDto(true);
    }

    /// <summary>
    /// Last known position of each ruta on a day (today when empty), for the live map: the one in memory, or the last
    /// saved point after a restart or for a past day. Rutas without readings are left out.
    /// </summary>
    public async Task<IReadOnlyList<LiveLocationDto>> LiveAsync(DateOnly? date, CancellationToken ct)
    {
        var day = date ?? clock.Today;
        var rutaDias = await db.RutaDias.AsNoTracking()
            .Where(d => d.Date == day)
            .Select(d => new
            {
                d.Id,
                d.RutaId,
                d.Ruta!.Code,
                d.Ruta.Color,
                // The chofer the office assigned that day, or the one whose usual ruta it is.
                Operador = d.Operador != null ? d.Operador.Name
                    : db.Operadores.Where(o => o.UsualRutaId == d.RutaId && o.Active).Select(o => o.Name).FirstOrDefault(),
            })
            .ToListAsync(ct);
        var ids = rutaDias.Select(d => d.Id).ToList();
        var saved = await db.ChoferLocations.AsNoTracking()
            .Where(u => ids.Contains(u.RutaDiaId))
            .GroupBy(u => u.RutaDiaId)
            .Select(g => g.OrderByDescending(u => u.RecordedAt).First())
            .ToDictionaryAsync(u => u.RutaDiaId, ct);

        // Read now, so a change of Gps.SinSenalMinutos also applies to the positions already in memory.
        var noSignal = await settings.GetIntegerAsync(SettingKeys.GpsNoSignalMinutes, ct);
        var result = new List<LiveLocationDto>();
        foreach (var d in rutaDias)
        {
            if (live.Get(d.Id) is { } inMemory)
            {
                result.Add(inMemory with { SignalLostAt = PanelRules.SignalLostAt(inMemory.At, noSignal) });
            }
            else if (saved.GetValueOrDefault(d.Id) is { } point)
            {
                result.Add(new LiveLocationDto(d.Id, day, d.RutaId, d.Code, d.Color, d.Operador,
                    point.Location.Y, point.Location.X, point.RecordedAt, point.AccuracyMeters, Live: false,
                    SignalLostAt: PanelRules.SignalLostAt(point.RecordedAt, noSignal)));
            }
        }
        return result;
    }

    /// <summary>Where the chofer went on a RutaDia, in order.</summary>
    public async Task<TrackDto> TrackAsync(int rutaDiaId, CancellationToken ct)
    {
        var date = await db.RutaDias.AsNoTracking().Where(d => d.Id == rutaDiaId).Select(d => (DateOnly?)d.Date).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("La ruta del día no existe.");
        var points = await db.ChoferLocations.AsNoTracking()
            .Where(u => u.RutaDiaId == rutaDiaId)
            .OrderBy(u => u.RecordedAt)
            .Select(u => new { u.Location, u.RecordedAt })
            .ToListAsync(ct);
        return new TrackDto(rutaDiaId, date, points.Select(p => new TrackPointDto(p.Location.Y, p.Location.X, p.RecordedAt)).ToList());
    }
}
