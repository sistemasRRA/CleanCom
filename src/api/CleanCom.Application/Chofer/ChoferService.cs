using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Application.Rutas;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Geography;
using CleanCom.Domain.Operations;
using CleanCom.Domain.People;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Chofer;

/// <summary>
/// What the chofer app shows and records (docs/pantallas/apps/chofer.md). The chofer is the operador linked to the
/// signed-in user.
/// </summary>
public class ChoferService(
    ICleanComDbContext db,
    ICurrentUser currentUser,
    IBusinessClock clock,
    ISettingService settings,
    RutaDiaService plans)
{
    /// <summary>The chofer's ruta on a day, or why there is none.</summary>
    private sealed record Resolved(Operador? Operador, Ruta? Ruta, RutaDia? RutaDia, bool Assigned, string? Problem);

    /// <summary>
    /// Paradas of the chofer's ruta on a day (today in Mexico City when empty). The ruta is the one the office assigned
    /// to this operador that day; otherwise their usual ruta, unless the office gave that day to another operador.
    /// </summary>
    public async Task<ChoferRutaDto> RutaAsync(DateOnly? date, CancellationToken ct)
    {
        var day = date ?? clock.Today;
        var label = AgendaCalculator.Format(day);
        var resolved = await ResolveAsync(day, ct);
        if (resolved.Problem is not null || resolved.Ruta is null)
        {
            return new ChoferRutaDto(day, label, resolved.Operador?.Name, null, false, resolved.Problem, []);
        }
        var ruta = resolved.Ruta;
        var rutaInfo = new ChoferRutaInfoDto(ruta.Id, ruta.Code, ruta.Name, ruta.Color);
        var orderRequired = await settings.GetBooleanAsync(SettingKeys.VisitOrderRequired, ct);
        var tolerance = await settings.GetIntegerAsync(SettingKeys.GpsToleranceMeters, ct);
        var isToday = day == clock.Today;

        var nonWorking = await db.NonWorkingDays.AsNoTracking()
            .Where(d => d.Active && d.Date == day).Select(d => d.Reason).FirstOrDefaultAsync(ct);
        if (resolved.RutaDia is not { } rutaDia)
        {
            return new ChoferRutaDto(day, label, resolved.Operador!.Name, rutaInfo, false,
                nonWorking is null ? $"La ruta {ruta.Code} no tiene paradas el {label}." : $"Día inhábil: {nonWorking}.", [],
                isToday, null, orderRequired, tolerance);
        }

        // Same order and estimated times as the Rutas screen.
        var plan = await plans.PlanAsync(rutaDia.Id, ct);
        var details = await db.Paradas.AsNoTracking()
            .Where(p => p.RutaDiaId == rutaDia.Id && p.Active)
            .Select(p => new
            {
                p.Id,
                p.EnCaminoAt,
                p.ArrivedAt,
                p.ArrivedDistanceMeters,
                p.ArrivedOutsideRadius,
                Phone = p.Pedido == null ? null : p.Pedido.Invitado!.WhatsAppPhone,
                Lineas = p.Pedido == null ? new List<string>() : p.Pedido.LineasServicio.Select(l => l.LineaServicio!.Name).ToList(),
                Interior = p.Direccion == null ? null : p.Direccion.InteriorNumber,
                References = p.Direccion == null ? null : p.Direccion.References,
            })
            .ToDictionaryAsync(p => p.Id, ct);

        var paradas = plan.Paradas.Select(p =>
        {
            var d = details[p.Id];
            var address = p.Address is not null && d.Interior is not null ? InsertInterior(p.Address, d.Interior) : p.Address;
            return new ChoferParadaDto(
                p.Id, p.Order, p.Kind, p.Category, p.State, p.Turno, p.SpecificTime, p.Before, p.Priority, p.PedidoId,
                p.Name, d.Phone, address, d.References, p.ZonaCode, p.Lat, p.Lng, p.GarmentsApprox, d.Lineas, p.Notes,
                p.Activity, p.Viaje, p.Arrival, d.EnCaminoAt, d.ArrivedAt, d.ArrivedDistanceMeters, d.ArrivedOutsideRadius);
        }).ToList();

        var next = isToday ? ChoferFlow.Next(Flow(paradas)) : null;
        var notice = nonWorking is not null ? $"Día inhábil: {nonWorking}." : paradas.Count == 0 ? $"La ruta {ruta.Code} no tiene paradas el {label}." : null;
        return new ChoferRutaDto(day, label, resolved.Operador!.Name, rutaInfo, resolved.Assigned, notice, paradas,
            isToday, next?.Id, orderRequired, tolerance);
    }

    /// <summary>"En camino": the chofer leaves for this parada (step 3). The notice to the invitado arrives in week 6.</summary>
    public async Task<ChoferRutaDto> EnCaminoAsync(int paradaId, ChoferLocationRequest r, CancellationToken ct)
    {
        var (parada, ordered, orderRequired) = await LoadForFlowAsync(paradaId, ct);
        ChoferFlow.EnsureCanStart(ordered.First(p => p.Id == paradaId), ordered, orderRequired);
        parada.State = ParadaState.EnCamino;
        parada.EnCaminoAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return await RutaAsync(clock.Today, ct);
    }

    /// <summary>
    /// "Llegué": the real arrival time (step 4). Farther than Gps.ToleranciaMetros it asks the chofer to confirm,
    /// and once confirmed the office sees it marked.
    /// </summary>
    public async Task<ChoferRutaDto> ArriveAsync(int paradaId, ArriveRequest r, CancellationToken ct)
    {
        var (parada, ordered, orderRequired) = await LoadForFlowAsync(paradaId, ct);
        var target = ordered.First(p => p.Id == paradaId);
        ChoferFlow.EnsureCanArrive(target, ordered, orderRequired);

        var phone = r.Lat is { } lat && r.Lng is { } lng ? Geo.CreatePoint(lat, lng) : null;
        var place = await ParadaLocationAsync(paradaId, ct);
        int? distance = phone is not null && place is not null
            ? ChoferFlow.DistanceMeters(new GeoPoint(phone.Y, phone.X), new GeoPoint(place.Y, place.X))
            : null;
        var tolerance = await settings.GetIntegerAsync(SettingKeys.GpsToleranceMeters, ct);
        var inside = ChoferFlow.WithinTolerance(distance, tolerance);
        if (!inside && !r.ConfirmOutsideRadius)
        {
            throw new ConfirmationRequiredException(
                $"Estás a {Distance(distance!.Value)} de {target.Name}; la llegada se registra a menos de {tolerance} m. ¿Registrar de todos modos? Oficina lo verá.");
        }

        parada.State = ParadaState.EnSitio;
        parada.EnCaminoAt ??= clock.UtcNow;
        parada.ArrivedAt = clock.UtcNow;
        parada.ArrivedLocation = phone;
        parada.ArrivedDistanceMeters = distance;
        parada.ArrivedOutsideRadius = !inside;
        await db.SaveChangesAsync(ct);
        return await RutaAsync(clock.Today, ct);
    }

    /// <summary>The parada (tracked) and today's ruta in the assigned order; the parada must be in the chofer's ruta today.</summary>
    private async Task<(Parada Parada, IReadOnlyList<FlowParada> Ordered, bool OrderRequired)> LoadForFlowAsync(int paradaId, CancellationToken ct)
    {
        var resolved = await ResolveAsync(clock.Today, ct);
        if (resolved.RutaDia is not { } rutaDia)
        {
            throw new DomainException(resolved.Problem ?? "Hoy no tienes paradas.");
        }
        var parada = await db.Paradas.FirstOrDefaultAsync(p => p.Id == paradaId && p.Active, ct)
            ?? throw new NotFoundException("La parada no existe.");
        if (parada.RutaDiaId != rutaDia.Id)
        {
            throw new DomainException("Esta parada no es de tu ruta de hoy.");
        }
        var plan = await plans.PlanAsync(rutaDia.Id, ct);
        var ordered = plan.Paradas.Select(p => new FlowParada(p.Id, p.Order, p.Name ?? "", p.State)).ToList();
        return (parada, ordered, await settings.GetBooleanAsync(SettingKeys.VisitOrderRequired, ct));
    }

    private async Task<NetTopologySuite.Geometries.Point?> ParadaLocationAsync(int paradaId, CancellationToken ct) =>
        await db.Paradas.AsNoTracking().Where(p => p.Id == paradaId)
            .Select(p => p.Direccion != null && p.Direccion.Location != null ? p.Direccion.Location
                : p.Location != null ? p.Location
                : p.Lugar != null ? p.Lugar.Location : null)
            .FirstOrDefaultAsync(ct);

    private async Task<Resolved> ResolveAsync(DateOnly day, CancellationToken ct)
    {
        var userId = currentUser.Id ?? throw new UnauthorizedException("La sesión no es válida.");
        var operador = await db.Operadores.AsNoTracking().FirstOrDefaultAsync(o => o.UserId == userId && o.Active, ct);
        if (operador is null)
        {
            return new Resolved(null, null, null, false, "Tu usuario no está ligado a un operador. Pide a oficina que lo ligue en Usuarios.");
        }

        var rutaDia = await db.RutaDias.AsNoTracking().Include(d => d.Ruta)
            .FirstOrDefaultAsync(d => d.Date == day && d.OperadorId == operador.Id, ct);
        var assigned = rutaDia is not null;
        if (rutaDia is null && operador.UsualRutaId is not null)
        {
            rutaDia = await db.RutaDias.AsNoTracking().Include(d => d.Ruta)
                .FirstOrDefaultAsync(d => d.Date == day && d.RutaId == operador.UsualRutaId, ct);
            if (rutaDia?.OperadorId is not null)
            {
                return new Resolved(operador, null, null, false, "Oficina asignó tu ruta habitual a otro chofer este día.");
            }
        }

        var ruta = rutaDia?.Ruta ?? (operador.UsualRutaId is null ? null
            : await db.Rutas.AsNoTracking().FirstOrDefaultAsync(r => r.Id == operador.UsualRutaId, ct));
        return ruta is null
            ? new Resolved(operador, null, null, false, "No tienes ruta asignada. Pide a oficina que te asigne una ruta habitual.")
            : new Resolved(operador, ruta, rutaDia, assigned, null);
    }

    private static List<FlowParada> Flow(IEnumerable<ChoferParadaDto> paradas) =>
        paradas.Select(p => new FlowParada(p.Id, p.Order, p.InvitadoName ?? "", p.State)).ToList();

    private static string InsertInterior(string address, string interior)
    {
        var comma = address.IndexOf(',');
        return comma < 0 ? $"{address} int. {interior}" : $"{address[..comma]} int. {interior}{address[comma..]}";
    }

    private static string Distance(int meters) => meters >= 1000 ? $"{meters / 1000.0:0.0} km" : $"{meters} m";
}
