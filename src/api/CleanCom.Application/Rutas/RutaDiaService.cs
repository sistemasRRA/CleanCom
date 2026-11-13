using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Rutas;

/// <summary>
/// Planning of the rutas of a day (docs/pantallas/oficina/rutas.md): chofer, order, estimated times, return and alerts.
/// Times and km are estimated by <see cref="RoutePlanner"/> until Google Route Optimization is connected.
/// </summary>
public class RutaDiaService(
    ICleanComDbContext db,
    ISettingService settings,
    IBusinessClock clock,
    IValidator<PatchParadaRequest> patchValidator)
{
    public async Task<RutasDiaDto> DayAsync(DateOnly? date, CancellationToken ct)
    {
        var day = date ?? clock.Today;
        var context = await ContextAsync(day, ct);
        var rutas = await db.Rutas.AsNoTracking().Where(r => r.Active).OrderBy(r => r.Code).ToListAsync(ct);
        var rutaDias = await db.RutaDias.AsNoTracking().Where(d => d.Date == day).ToListAsync(ct);

        var plans = new List<RutaPlanDto>();
        foreach (var ruta in rutas)
        {
            plans.Add(await PlanAsync(ruta, day, rutaDias.FirstOrDefault(d => d.RutaId == ruta.Id), context, ct));
        }
        var nonWorking = await db.NonWorkingDays.AsNoTracking().Where(d => d.Active && d.Date == day).Select(d => d.Reason).FirstOrDefaultAsync(ct);
        return new RutasDiaDto(day, AgendaCalculator.Format(day), nonWorking,
            $"Horas y km estimados en línea recta × {context.Base.DistanceFactor:0.##} a {context.Base.SpeedKmh:0.#} km/h, hasta conectar Google.",
            plans);
    }

    /// <summary>The chofer of a ruta on a day; null goes back to the operador whose usual ruta it is.</summary>
    public async Task<RutaPlanDto> AssignOperadorAsync(int rutaId, DateOnly date, AssignOperadorRequest r, CancellationToken ct)
    {
        var ruta = await db.Rutas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == rutaId && x.Active, ct)
            ?? throw new NotFoundException($"La ruta {rutaId} no existe o está inactiva.");
        if (date < clock.Today)
        {
            throw new DomainException("No se puede cambiar el chofer de un día que ya pasó.");
        }
        if (r.OperadorId is { } operadorId)
        {
            var operador = await db.Operadores.AsNoTracking().FirstOrDefaultAsync(o => o.Id == operadorId && o.Active, ct)
                ?? throw new NotFoundException("El operador no existe o está inactivo.");
            var other = await db.RutaDias.AsNoTracking().Include(d => d.Ruta)
                .FirstOrDefaultAsync(d => d.Date == date && d.OperadorId == operadorId && d.RutaId != rutaId, ct);
            if (other is not null)
            {
                throw new ConflictException($"{operador.Name} ya tiene la ruta {other.Ruta!.Code} el {AgendaCalculator.Format(date)}.");
            }
        }

        var rutaDia = await db.RutaDias.FirstOrDefaultAsync(d => d.RutaId == rutaId && d.Date == date, ct);
        if (rutaDia is null)
        {
            rutaDia = new RutaDia { RutaId = rutaId, Date = date };
            db.RutaDias.Add(rutaDia);
        }
        rutaDia.OperadorId = r.OperadorId;
        await db.SaveChangesAsync(ct);
        return await PlanAsync(ruta, date, rutaDia, await ContextAsync(date, ct), ct);
    }

    /// <summary>
    /// Orders the ruta (POST /rutas/dia/{id}/optimizar): started paradas stay first, pinned ones keep their place,
    /// viajes stay together and in order.
    /// </summary>
    public async Task<RutaPlanDto> OptimizeAsync(int rutaDiaId, CancellationToken ct)
    {
        var rutaDia = await db.RutaDias.Include(d => d.Ruta).FirstOrDefaultAsync(d => d.Id == rutaDiaId, ct)
            ?? throw new NotFoundException($"La ruta del día {rutaDiaId} no existe.");
        var context = await ContextAsync(rutaDia.Date, ct);
        var paradas = await db.Paradas.Where(p => p.RutaDiaId == rutaDiaId && p.Active).ToListAsync(ct);
        var rows = await RowsAsync(rutaDiaId, ct);
        var planSettings = await PlanSettingsAsync(rutaDia.Ruta!, rutaDia.Date, context, ct);
        var ordered = Sorted(rows, context);
        var optimized = RoutePlanner.Optimize(ordered.Select(x => ToStop(x, planSettings, context)).ToList(), planSettings);

        for (var i = 0; i < optimized.Count; i++)
        {
            paradas.First(p => p.Id == optimized[i].Id).Position = i + 1;
        }
        await db.SaveChangesAsync(ct);
        return await PlanAsync(rutaDia.Ruta!, rutaDia.Date, rutaDia, context, ct);
    }

    /// <summary>Moves a parada (its whole viaje if it has one) and/or pins it. Moving it by hand pins it.</summary>
    public async Task<RutaPlanDto> PatchParadaAsync(int paradaId, PatchParadaRequest r, CancellationToken ct)
    {
        await patchValidator.ValidateAndThrowAsync(r, ct);
        var parada = await db.Paradas.AsNoTracking().Include(p => p.RutaDia).ThenInclude(d => d!.Ruta)
            .FirstOrDefaultAsync(p => p.Id == paradaId && p.Active, ct)
            ?? throw new NotFoundException($"La parada {paradaId} no existe.");
        var rutaDia = parada.RutaDia!;
        var context = await ContextAsync(rutaDia.Date, ct);
        var paradas = await db.Paradas.Where(p => p.RutaDiaId == rutaDia.Id && p.Active).ToListAsync(ct);
        var order = Sorted(await RowsAsync(rutaDia.Id, ct), context).Select(x => x.Id).ToList();

        var block = parada.ViajeId is { } viajeId
            ? paradas.Where(p => p.ViajeId == viajeId).OrderBy(p => p.ViajeStep).Select(p => p.Id).ToList()
            : [parada.Id];
        if (r.Position is { } position)
        {
            order.RemoveAll(block.Contains);
            var index = Math.Clamp(position - 1, 0, order.Count);
            // Never inside another viaje: after it.
            var byId = paradas.ToDictionary(p => p.Id);
            while (index > 0 && index < order.Count && byId[order[index]].ViajeId is { } v && byId[order[index - 1]].ViajeId == v)
            {
                index++;
            }
            order.InsertRange(index, block);
        }
        for (var i = 0; i < order.Count; i++)
        {
            paradas.First(p => p.Id == order[i]).Position = i + 1;
        }
        foreach (var p in paradas.Where(p => block.Contains(p.Id)))
        {
            p.PositionFixed = r.Fixed ?? true;
        }
        await db.SaveChangesAsync(ct);
        return await PlanAsync(rutaDia.Ruta!, rutaDia.Date, rutaDia, context, ct);
    }

    // ---------------- plan ----------------

    /// <summary>General values for a day, read once.</summary>
    private sealed record DayContext(
        TimeRange Morning,
        TimeRange Afternoon,
        int SpecificWindow,
        int RecoleccionMinutes,
        int EntregaMinutes,
        int TransferMinutes,
        TimeOnly MorningEnd,
        PlanSettings Base);

    private async Task<DayContext> ContextAsync(DateOnly date, CancellationToken ct)
    {
        var (lat, lng) = SettingValue.AsCoordinate(await settings.GetAsync(SettingKeys.BaseLocation, null, ct));
        var morning = await settings.GetTimeRangeAsync(SettingKeys.TurnoMorning, ct);
        var basePlan = new PlanSettings(
            new GeoPoint(lat, lng),
            Minutes(await settings.GetTimeAsync(SettingKeys.RutaBaseDeparture, ct)),
            0,
            Minutes(await settings.GetTimeAsync(SettingKeys.LunchFrom, ct)),
            await settings.GetIntegerAsync(SettingKeys.LunchMinutes, ct),
            (double)await settings.GetDecimalAsync(SettingKeys.RutaAverageSpeed, ct),
            (double)await settings.GetDecimalAsync(SettingKeys.RutaDistanceFactor, ct),
            await settings.GetIntegerAsync(SettingKeys.UnidadGarmentCapacity, ct));
        return new DayContext(
            morning,
            await settings.GetTimeRangeAsync(SettingKeys.TurnoAfternoon, ct),
            await settings.GetIntegerAsync(SettingKeys.ParadaSpecificTimeWindow, ct),
            await settings.GetIntegerAsync(SettingKeys.ParadaRecoleccionMinutes, ct),
            await settings.GetIntegerAsync(SettingKeys.ParadaEntregaMinutes, ct),
            await settings.GetIntegerAsync(SettingKeys.ParadaTransferMinutes, ct),
            morning.End,
            basePlan);
    }

    private async Task<PlanSettings> PlanSettingsAsync(Ruta ruta, DateOnly date, DayContext context, CancellationToken ct)
    {
        var maxReturnKey = date.DayOfWeek == DayOfWeek.Saturday ? SettingKeys.RutaMaxReturnTimeSaturday : SettingKeys.RutaMaxReturnTime;
        return context.Base with { MaxReturn = Minutes(await settings.GetTimeAsync(maxReturnKey, ct, ruta.Id)) };
    }

    private async Task<RutaPlanDto> PlanAsync(Ruta ruta, DateOnly date, RutaDia? rutaDia, DayContext context, CancellationToken ct)
    {
        var plan = await PlanSettingsAsync(ruta, date, context, ct);
        var cupo = await settings.GetIntegerAsync(SettingKeys.RutaCupoParadas, ct, ruta.Id);

        // Chofer: the one assigned for the day, else the operador whose usual ruta this is (if not busy in another ruta).
        int? operadorId = rutaDia?.OperadorId;
        var usual = false;
        if (operadorId is null)
        {
            var busy = await db.RutaDias.AsNoTracking().Where(d => d.Date == date && d.OperadorId != null).Select(d => d.OperadorId!.Value).ToListAsync(ct);
            operadorId = await db.Operadores.AsNoTracking()
                .Where(o => o.Active && o.UsualRutaId == ruta.Id && !busy.Contains(o.Id))
                .OrderBy(o => o.Name).Select(o => (int?)o.Id).FirstOrDefaultAsync(ct);
            usual = operadorId is not null;
        }
        var operadorName = operadorId is null ? null
            : await db.Operadores.AsNoTracking().Where(o => o.Id == operadorId).Select(o => o.Name).FirstOrDefaultAsync(ct);

        var rows = rutaDia is null ? [] : await RowsAsync(rutaDia.Id, ct);
        var ordered = Sorted(rows, context);
        var schedule = RoutePlanner.Schedule(ordered.Select(x => ToStop(x, plan, context)).ToList(), plan);

        var paradas = ordered.Select((x, i) =>
        {
            var s = schedule.Stops[i];
            var (start, end) = Window(x.Turno, x.SpecificTime, plan, context);
            return new RutaPlanParadaDto(
                x.Id, s.Order, x.Kind, x.Category, x.State, x.Turno, x.SpecificTime, x.Priority,
                x.Name, x.Address, x.ZonaCode, x.Location?.Lat, x.Location?.Lng,
                Time(start), Time(end), Time(s.Arrival), Time(s.Leave), s.WaitMinutes, s.KmFromPrevious,
                s.OutOfWindow, s.NoLocation, s.LunchAfter, x.Placement, x.PositionFixed, x.ViajeId,
                x.ViajeId is { } v ? $"{x.ViajeName} · paso {x.ViajeStep} de {rows.Count(r => r.ViajeId == v)}" : null,
                x.PedidoId, x.ParadaFijaId, x.Garments, x.Activity, x.Notes);
        }).ToList();

        var alerts = new List<string>();
        if (paradas.Count > 0 && operadorId is null)
        {
            alerts.Add("Sin chofer asignado.");
        }
        if (schedule.Overtime && paradas.Count > 0)
        {
            alerts.Add($"Regreso estimado a Leones a las {Time(schedule.ReturnAt):HH\\:mm}, después de las {Time(plan.MaxReturn):HH\\:mm}: horas extra.");
        }
        alerts.AddRange(paradas.Where(p => p.OutOfWindow).Select(p =>
            $"{p.Order}. {p.Name} queda a las {p.Arrival:HH\\:mm}, fuera de su ventana ({p.WindowStart:HH\\:mm} a {p.WindowEnd:HH\\:mm})."));
        if (schedule.OverCapacity)
        {
            alerts.Add($"Prendas estimadas: {schedule.Garments}, más de las {plan.Capacity} que caben en la unidad.");
        }
        var occupied = rows.Count(x => Parada.TakesCupo(x.Kind));
        if (occupied > cupo)
        {
            alerts.Add($"{occupied} paradas, más que el cupo de {cupo}.");
        }
        var noLocation = paradas.Where(p => p.NoLocation).Select(p => p.Order.ToString()).ToList();
        if (noLocation.Count > 0)
        {
            alerts.Add($"Sin coordenadas, su traslado no se cuenta: parada{(noLocation.Count > 1 ? "s" : "")} {string.Join(", ", noLocation)}.");
        }
        var optimized = rows.Count > 0 && rows.All(x => x.Position > 0);
        var unplaced = rows.Count(x => x.Position == 0);
        if (unplaced > 0 && unplaced < rows.Count)
        {
            alerts.Add($"{unplaced} parada(s) nueva(s) sin acomodar: presiona Optimizar.");
        }

        return new RutaPlanDto(
            ruta.Id, ruta.Code, ruta.Name, ruta.Color, rutaDia?.Id, operadorId, operadorName, usual,
            occupied, cupo, optimized, Time(plan.Departure), schedule.LunchStart is { } l ? Time(l) : null,
            Time(schedule.ReturnAt), Time(plan.MaxReturn), schedule.Km, schedule.Garments, plan.Capacity, alerts, paradas);
    }

    private sealed record Row(
        int Id,
        int Position,
        bool PositionFixed,
        ParadaKind Kind,
        ParadaCategory? Category,
        ParadaState State,
        Turno Turno,
        TimeOnly? SpecificTime,
        bool Priority,
        ParadaPlacement Placement,
        int? ViajeId,
        int? ViajeStep,
        string? ViajeName,
        int? PedidoId,
        int? ParadaFijaId,
        string? Name,
        string? Address,
        string? ZonaCode,
        GeoPoint? Location,
        int? Garments,
        string? Activity,
        string? Notes);

    private async Task<List<Row>> RowsAsync(int rutaDiaId, CancellationToken ct)
    {
        var raw = await db.Paradas.AsNoTracking()
            .Where(p => p.RutaDiaId == rutaDiaId && p.Active)
            .Select(p => new
            {
                p.Id, p.Position, p.PositionFixed, p.Kind, p.Category, p.State, p.Turno, p.Placement, p.ViajeId, p.ViajeStep,
                ViajeName = p.Viaje == null ? null : p.Viaje.Name,
                p.PedidoId, p.ParadaFijaId, p.Title, p.Activity, p.Notes,
                SpecificTime = p.Pedido == null ? null : p.Pedido.SpecificTime,
                Priority = p.Pedido != null && p.Pedido.Priority,
                PedidoGarments = p.Pedido == null ? null : p.Pedido.GarmentsApprox,
                FijaGarments = p.ParadaFija == null ? null : p.ParadaFija.GarmentsApprox,
                InvitadoName = p.Pedido == null ? null : p.Pedido.Invitado!.Name,
                LugarName = p.Lugar == null ? null : p.Lugar.Name,
                Street = p.Direccion == null ? null : p.Direccion.Street,
                Exterior = p.Direccion == null ? null : p.Direccion.ExteriorNumber,
                Neighborhood = p.Direccion == null ? null : p.Direccion.Neighborhood,
                ZonaCode = p.Direccion == null || p.Direccion.Zona == null ? null : p.Direccion.Zona.Code,
                DireccionLocation = p.Direccion == null ? null : p.Direccion.Location,
                LugarAddress = p.Lugar == null ? null : p.Lugar.Address,
                LugarLocation = p.Lugar == null ? null : p.Lugar.Location,
                ParadaAddress = p.Address,
                ParadaLocation = p.Location,
            })
            .ToListAsync(ct);

        return raw.Select(p =>
        {
            var point = p.DireccionLocation ?? p.ParadaLocation ?? p.LugarLocation;
            var name = p.InvitadoName ?? p.Title ?? p.LugarName;
            if (p.InvitadoName is null && p.Title is not null && p.LugarName is not null)
            {
                name = $"{p.Title} · {p.LugarName}";
            }
            var address = p.Street is not null
                ? $"{p.Street}{(p.Exterior is null ? "" : $" {p.Exterior}")}{(p.Neighborhood is null ? "" : $", {p.Neighborhood}")}"
                : p.ParadaAddress ?? p.LugarAddress;
            // A viaje's prendas count once, on its first paso.
            var garments = p.PedidoGarments ?? (p.ViajeStep is null or 1 ? p.FijaGarments : null);
            return new Row(p.Id, p.Position, p.PositionFixed, p.Kind, p.Category, p.State, p.Turno, p.SpecificTime, p.Priority,
                p.Placement, p.ViajeId, p.ViajeStep, p.ViajeName, p.PedidoId, p.ParadaFijaId, name, address, p.ZonaCode,
                point is null ? null : new GeoPoint(point.Y, point.X), garments, p.Activity, p.Notes);
        }).ToList();
    }

    /// <summary>The same order the chofer sees: positions once optimized, otherwise provisional.</summary>
    private static IReadOnlyList<Row> Sorted(IEnumerable<Row> rows, DayContext context) =>
        ProvisionalOrder.Sort(rows,
            x => new OrderKey(x.Position, x.Placement, x.Turno, x.SpecificTime, x.Priority, x.ViajeId, x.ViajeStep, x.Id),
            context.MorningEnd);

    private static PlanStop ToStop(Row x, PlanSettings plan, DayContext context)
    {
        var (start, end) = Window(x.Turno, x.SpecificTime, plan, context);
        var minutes = x.Kind switch
        {
            ParadaKind.Recoleccion => context.RecoleccionMinutes,
            ParadaKind.Entrega => context.EntregaMinutes,
            ParadaKind.RecoleccionYEntrega => context.RecoleccionMinutes + context.EntregaMinutes,
            ParadaKind.Salida or ParadaKind.Regreso or ParadaKind.Comida => 0,
            _ => context.TransferMinutes,
        };
        return new PlanStop(x.Id, x.Location, minutes, start, end, x.Placement, x.ViajeId, x.ViajeStep, x.Position,
            x.PositionFixed, x.State != ParadaState.Pendiente, x.Garments ?? 0);
    }

    /// <summary>docs/03-reglas-de-negocio.md, Rutas, rule 2.</summary>
    private static (int Start, int End) Window(Turno turno, TimeOnly? specific, PlanSettings plan, DayContext context) => turno switch
    {
        Turno.Manana => (Minutes(context.Morning.Start), Minutes(context.Morning.End)),
        Turno.Tarde => (Minutes(context.Afternoon.Start), Minutes(context.Afternoon.End)),
        Turno.HoraEspecifica when specific is { } t => (Minutes(t) - context.SpecificWindow, Minutes(t) + context.SpecificWindow),
        _ => (plan.Departure, plan.MaxReturn),
    };

    private static int Minutes(TimeOnly t) => t.Hour * 60 + t.Minute;

    private static TimeOnly Time(int minutes) => new(Math.Clamp(minutes, 0, 24 * 60 - 1) / 60, Math.Clamp(minutes, 0, 24 * 60 - 1) % 60);
}
