using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Application.ParadasFijas;
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
    IValidator<PatchParadaRequest> patchValidator,
    IValidator<AddToRutaRequest> addValidator)
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

    /// <summary>Moves a parada and/or pins it. Moving it by hand pins it; the pasos of a viaje must keep their order.</summary>
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

        if (r.Position is { } position)
        {
            order.Remove(parada.Id);
            order.Insert(Math.Clamp(position - 1, 0, order.Count), parada.Id);
            if (parada.ViajeId is { } viajeId)
            {
                var steps = order.Select(id => paradas.First(p => p.Id == id)).Where(p => p.ViajeId == viajeId).Select(p => p.ViajeStep ?? 0).ToList();
                if (!steps.SequenceEqual(steps.Order()))
                {
                    var name = await db.Viajes.Where(v => v.Id == viajeId).Select(v => v.Name).FirstAsync(ct);
                    throw new DomainException($"{name}: el paso {parada.ViajeStep} no puede quedar antes o después de ese lugar; los pasos van en orden.");
                }
            }
        }
        for (var i = 0; i < order.Count; i++)
        {
            paradas.First(p => p.Id == order[i]).Position = i + 1;
        }
        paradas.First(p => p.Id == parada.Id).PositionFixed = r.Fixed ?? true;
        await db.SaveChangesAsync(ct);
        return await PlanAsync(rutaDia.Ruta!, rutaDia.Date, rutaDia, context, ct);
    }

    /// <summary>
    /// "Parada de un día" (<paramref name="viaje"/> false, one paso) or "Viaje de un día" (two or more pasos in order).
    /// It takes cupo like any parada; going over the cupo shows in the alerts.
    /// </summary>
    public async Task<RutaPlanDto> AddAsync(AddToRutaRequest r, bool viaje, CancellationToken ct)
    {
        await addValidator.ValidateAndThrowAsync(r, ct);
        if (viaje && r.Pasos.Count < 2)
        {
            throw new DomainException("Un viaje lleva al menos dos pasos; para un solo lugar agrega una parada de un día.");
        }
        if (!viaje && r.Pasos.Count != 1)
        {
            throw new DomainException("Una parada de un día lleva un solo lugar; para varios agrega un viaje de un día.");
        }
        var ruta = await db.Rutas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.RutaId && x.Active, ct)
            ?? throw new NotFoundException($"La ruta {r.RutaId} no existe o está inactiva.");
        if (r.Date < clock.Today)
        {
            throw new DomainException("No se pueden agregar paradas a un día que ya pasó.");
        }
        if (await db.NonWorkingDays.AnyAsync(d => d.Active && d.Date == r.Date, ct))
        {
            throw new DomainException($"El {AgendaCalculator.Format(r.Date)} es día inhábil.");
        }
        var lugarIds = r.Pasos.Where(p => p.LugarId is not null).Select(p => p.LugarId!.Value).Distinct().ToList();
        if (lugarIds.Count > 0 && await db.Lugares.CountAsync(l => lugarIds.Contains(l.Id) && l.Active, ct) != lugarIds.Count)
        {
            throw new DomainException("Algún lugar no existe o está inactivo.");
        }

        // Built like a parada fija that is never saved: same pasos, same viaje, no link to a parada fija.
        var oneDay = new ParadaFija
        {
            Name = r.Name.Trim(),
            Category = r.Category,
            Kind = r.Kind,
            Turno = r.Turno,
            Placement = r.Placement,
            Carried = PasoMapper.Clean(r.Carried),
            DriverNotes = PasoMapper.Clean(r.DriverNotes),
        };
        for (var i = 0; i < r.Pasos.Count; i++)
        {
            var paso = new ParadaFijaPaso();
            PasoMapper.Apply(paso, r.Pasos[i], i + 1, r.Kind, r.Turno);
            oneDay.Pasos.Add(paso);
        }

        var rutaDia = await db.RutaDias.FirstOrDefaultAsync(d => d.RutaId == r.RutaId && d.Date == r.Date, ct);
        if (rutaDia is null)
        {
            rutaDia = new RutaDia { RutaId = r.RutaId, Date = r.Date };
            db.RutaDias.Add(rutaDia);
        }
        db.Paradas.AddRange(oneDay.BuildParadas(rutaDia));
        await db.SaveChangesAsync(ct);
        return await PlanAsync(ruta, r.Date, rutaDia, await ContextAsync(r.Date, ct), ct);
    }

    /// <summary>
    /// Takes a parada out of the ruta that day (baja lógica); a paso of a viaje takes out the whole viaje.
    /// A pedido's recolección is cancelled from the agenda, not here.
    /// </summary>
    public async Task<RutaPlanDto> RemoveParadaAsync(int paradaId, CancellationToken ct)
    {
        var parada = await db.Paradas.Include(p => p.RutaDia).ThenInclude(d => d!.Ruta)
            .FirstOrDefaultAsync(p => p.Id == paradaId && p.Active, ct)
            ?? throw new NotFoundException($"La parada {paradaId} no existe.");
        if (parada.PedidoId is not null)
        {
            throw new DomainException("Es la recolección de un pedido: elimínala desde la agenda.");
        }
        var paradas = parada.ViajeId is { } viajeId
            ? await db.Paradas.Include(p => p.Viaje).Where(p => p.ViajeId == viajeId && p.Active).ToListAsync(ct)
            : [parada];
        if (paradas.Any(p => p.State != ParadaState.Pendiente))
        {
            throw new DomainException("El chofer ya la inició; no se puede quitar.");
        }
        foreach (var p in paradas)
        {
            p.Active = false;
            p.Viaje?.Active = false;
        }
        await db.SaveChangesAsync(ct);
        var rutaDia = parada.RutaDia!;
        return await PlanAsync(rutaDia.Ruta!, rutaDia.Date, rutaDia, await ContextAsync(rutaDia.Date, ct), ct);
    }

    /// <summary>The plan of one ruta day (estimated times), for the chofer app.</summary>
    public async Task<RutaPlanDto> PlanAsync(int rutaDiaId, CancellationToken ct)
    {
        var rutaDia = await db.RutaDias.AsNoTracking().Include(d => d.Ruta).FirstAsync(d => d.Id == rutaDiaId, ct);
        return await PlanAsync(rutaDia.Ruta!, rutaDia.Date, rutaDia, await ContextAsync(rutaDia.Date, ct), ct);
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
            var (start, end) = Window(x.Turno, x.SpecificTime, x.Before, plan, context);
            return new RutaPlanParadaDto(
                x.Id, s.Order, x.Kind, x.Category, x.State, x.Turno, x.SpecificTime, x.Before, x.Priority,
                x.Name, x.Address, x.ZonaCode, x.Location?.Lat, x.Location?.Lng,
                Time(start), Time(end), Time(s.Arrival), Time(s.Leave), s.WaitMinutes, s.KmFromPrevious,
                s.OutOfWindow, s.NoLocation, s.LunchAfter, x.Placement, x.PositionFixed, x.ViajeId,
                x.ViajeId is { } v ? $"{x.ViajeName} · paso {x.ViajeStep} de {rows.Count(r => r.ViajeId == v)}" : null,
                x.PedidoId, x.ParadaFijaId, x.Garments, x.Activity, x.Notes, x.EnCaminoAt, x.ArrivedAt, x.ArrivedOutsideRadius);
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
        TimeOnly? Before,
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
        string? Notes,
        DateTimeOffset? EnCaminoAt,
        DateTimeOffset? ArrivedAt,
        bool ArrivedOutsideRadius);

    private async Task<List<Row>> RowsAsync(int rutaDiaId, CancellationToken ct)
    {
        var raw = await db.Paradas.AsNoTracking()
            .Where(p => p.RutaDiaId == rutaDiaId && p.Active)
            .Select(p => new
            {
                p.Id, p.Position, p.PositionFixed, p.Kind, p.Category, p.State, p.Turno, p.Before, p.Placement, p.ViajeId, p.ViajeStep,
                ViajeName = p.Viaje == null ? null : p.Viaje.Name,
                p.PedidoId, p.ParadaFijaId, p.Title, p.Activity, p.Notes, p.EnCaminoAt, p.ArrivedAt, p.ArrivedOutsideRadius,
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
            return new Row(p.Id, p.Position, p.PositionFixed, p.Kind, p.Category, p.State, p.Turno, p.SpecificTime, p.Before, p.Priority,
                p.Placement, p.ViajeId, p.ViajeStep, p.ViajeName, p.PedidoId, p.ParadaFijaId, name, address, p.ZonaCode,
                point is null ? null : new GeoPoint(point.Y, point.X), garments, p.Activity, p.Notes, p.EnCaminoAt, p.ArrivedAt, p.ArrivedOutsideRadius);
        }).ToList();
    }

    /// <summary>The same order the chofer sees: positions once optimized, otherwise provisional.</summary>
    private static IReadOnlyList<Row> Sorted(IEnumerable<Row> rows, DayContext context) =>
        ProvisionalOrder.Sort(rows,
            x => new OrderKey(x.Position, x.Placement, x.Turno, x.SpecificTime, x.Priority, x.ViajeId, x.ViajeStep, x.Id),
            context.MorningEnd);

    private static PlanStop ToStop(Row x, PlanSettings plan, DayContext context)
    {
        var (start, end) = Window(x.Turno, x.SpecificTime, x.Before, plan, context);
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

    /// <summary>docs/03-reglas-de-negocio.md, Rutas, rule 2. A latest time ("Antes: a las 2:00 p.m.") closes it earlier.</summary>
    private static (int Start, int End) Window(Turno turno, TimeOnly? specific, TimeOnly? before, PlanSettings plan, DayContext context)
    {
        var (start, end) = TurnoWindow(turno, specific, plan, context);
        return before is { } b ? (Math.Min(start, Minutes(b)), Math.Min(end, Minutes(b))) : (start, end);
    }

    private static (int Start, int End) TurnoWindow(Turno turno, TimeOnly? specific, PlanSettings plan, DayContext context) => turno switch
    {
        Turno.Manana => (Minutes(context.Morning.Start), Minutes(context.Morning.End)),
        Turno.Tarde => (Minutes(context.Afternoon.Start), Minutes(context.Afternoon.End)),
        Turno.HoraEspecifica when specific is { } t => (Minutes(t) - context.SpecificWindow, Minutes(t) + context.SpecificWindow),
        _ => (plan.Departure, plan.MaxReturn),
    };

    private static int Minutes(TimeOnly t) => t.Hour * 60 + t.Minute;

    private static TimeOnly Time(int minutes) => new(Math.Clamp(minutes, 0, 24 * 60 - 1) / 60, Math.Clamp(minutes, 0, 24 * 60 - 1) % 60);
}
