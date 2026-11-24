using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Application.ParadasFijas;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Geography;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Agenda;

/// <summary>
/// The single agenda engine (docs/03-reglas-de-negocio.md, Agenda). The office (Agendar) and the invitado app call
/// these same two operations; nothing about dates, cutoffs or cupo is decided anywhere else.
/// </summary>
public class AgendaService(
    ICleanComDbContext db,
    ISettingService settings,
    IBusinessClock clock,
    ZonaLocator locator,
    IAgendaLock agendaLock,
    PedidoService pedidos,
    FijaCalendar fijas,
    IValidator<AvailabilityRequest> availabilityValidator,
    IValidator<ScheduleRequest> scheduleValidator)
{
    /// <summary>Safety limit for the search; the answer explains when nothing is found.</summary>
    private const int SearchDays = 90;

    /// <summary>How many visit days to list after the chosen one.</summary>
    private const int OtherDays = 6;

    public async Task<AvailabilityDto> AvailabilityAsync(AvailabilityRequest r, CancellationToken ct)
    {
        await availabilityValidator.ValidateAndThrowAsync(r, ct);
        var location = await LocateAsync(r, ct);
        return await CalculateAsync(location, r.RegisteredAt ?? clock.UtcNow, ct);
    }

    /// <summary>
    /// The office agenda from <paramref name="from"/> for <paramref name="days"/> days (1 to 31): occupancy of each active
    /// ruta per day against its cupo, days off, and the scheduled pedidos. Without a date, the current week from Monday.
    /// </summary>
    public async Task<AgendaBoardDto> BoardAsync(DateOnly? from, int? days, CancellationToken ct)
    {
        var today = clock.Today;
        var start = from ?? today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var count = Math.Clamp(days ?? 7, 1, 31);
        var end = start.AddDays(count - 1);

        var rutas = await db.Rutas.AsNoTracking().Where(r => r.Active).OrderBy(r => r.Code)
            .Select(r => new BoardRutaDto(r.Id, r.Code, r.Name, r.Color)).ToListAsync(ct);
        var cupos = new Dictionary<int, int>();
        foreach (var ruta in rutas)
        {
            cupos[ruta.Id] = await settings.GetIntegerAsync(SettingKeys.RutaCupoParadas, ct, ruta.Id);
        }

        var occupancy = await db.Paradas.AsNoTracking()
            .Where(p => p.Active && p.RutaDia!.Date >= start && p.RutaDia.Date <= end && !Parada.KindsWithoutCupo.Contains(p.Kind))
            .GroupBy(p => new { p.RutaDia!.RutaId, p.RutaDia.Date })
            .Select(g => new { g.Key.RutaId, g.Key.Date, Count = g.Count() })
            .ToListAsync(ct);
        var nonWorking = await db.NonWorkingDays.AsNoTracking()
            .Where(d => d.Active && d.Date >= start && d.Date <= end)
            .ToDictionaryAsync(d => d.Date, d => d.Reason, ct);
        var pendingFijas = await fijas.PendingCupoAsync(null, start, end, ct);

        var boardDays = Enumerable.Range(0, count).Select(i =>
        {
            var date = start.AddDays(i);
            return new BoardDayDto(
                date,
                AgendaCalculator.Format(date),
                nonWorking.GetValueOrDefault(date),
                rutas.Select(r => new BoardRutaDayDto(
                    r.Id,
                    (occupancy.FirstOrDefault(o => o.RutaId == r.Id && o.Date == date)?.Count ?? 0) + pendingFijas.GetValueOrDefault((r.Id, date)),
                    cupos[r.Id])).ToList());
        }).ToList();

        var pedidoList = await pedidos.ListAsync(start, end, null, ct);
        return new AgendaBoardDto(start, end, today, rutas, boardDays, pedidoList);
    }

    public async Task<ScheduleResultDto> ScheduleAsync(ScheduleRequest r, PedidoOrigin origin, CancellationToken ct)
    {
        await scheduleValidator.ValidateAndThrowAsync(r, ct);
        if (origin == PedidoOrigin.App && (r.Priority || r.Turno == Turno.HoraEspecifica))
        {
            throw new DomainException("La hora específica y la prioridad solo las puede asignar oficina.");
        }
        var address = await db.Addresses.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == r.AddressId && d.InvitadoId == r.InvitadoId && d.Active && d.Invitado!.Active, ct)
            ?? throw new NotFoundException("La dirección no existe o no es de este invitado.");
        await EnsureLineasServicioAsync(r.LineaServicioIds, ct);

        var registeredAt = r.RegisteredAt ?? clock.UtcNow;
        var location = await LocateAsync(new AvailabilityRequest(address.Id, null, null, null, null, null), ct);
        var availability = await CalculateAsync(location, registeredAt, ct);
        if (availability.OutOfCoverage)
        {
            throw new DomainException("La dirección está fuera de cobertura. Asigna la zona a mano en la dirección del invitado y vuelve a agendar.");
        }

        var date = r.Date ?? availability.Date
            ?? throw new DomainException($"No hay días con cupo en los siguientes {SearchDays} días para la zona {availability.ZonaCode}.");
        if (r.Date is not null)
        {
            await EnsureDayCanBeChosenAsync(r.Date.Value, location.Zona!, availability, ct);
        }

        var rutaId = availability.RutaId!.Value;
        var cupo = availability.Cupo!.Value;
        await using var handle = await agendaLock.AcquireAsync(rutaId, date, ct);

        // Same calculation again inside the lock: someone may have taken the place meanwhile.
        var occupied = await OccupancyQuery(rutaId, date, date).CountAsync(ct)
            + (await fijas.PendingCupoAsync(rutaId, date, date, ct)).GetValueOrDefault((rutaId, date));
        if (occupied >= cupo)
        {
            var next = await CalculateAsync(location, registeredAt, ct);
            return new ScheduleResultDto(false, null, null, null, null,
                $"El {AgendaCalculator.Format(date)} ya no tiene cupo ({occupied} de {cupo}). Siguiente fecha disponible: "
                + (next.Date is null ? "ninguna en los próximos días." : $"{AgendaCalculator.Format(next.Date.Value)}, lugar {next.Place} de {next.Cupo}."),
                next);
        }

        var rutaDia = await db.RutaDias.FirstOrDefaultAsync(d => d.RutaId == rutaId && d.Date == date, ct);
        if (rutaDia is null)
        {
            rutaDia = new RutaDia { RutaId = rutaId, Date = date };
            db.RutaDias.Add(rutaDia);
        }

        var pedido = new Pedido
        {
            InvitadoId = r.InvitadoId,
            AddressId = address.Id,
            Etapa = PedidoEtapa.Agendado,
            GarmentsApprox = r.GarmentsApprox,
            Turno = r.Turno,
            SpecificTime = r.SpecificTime,
            Priority = r.Priority,
            DriverNotes = string.IsNullOrWhiteSpace(r.DriverNotes) ? null : r.DriverNotes.Trim(),
            RegisteredAt = registeredAt,
            Origin = origin,
            RecoleccionDate = date,
            EtapaSince = clock.UtcNow,
            Eventos =
            [
                new PedidoEvento
                {
                    Kind = PedidoEventoKind.Etapa,
                    ToEtapa = PedidoEtapa.Agendado,
                    Date = date,
                    Automatic = origin == PedidoOrigin.App,
                    Note = origin == PedidoOrigin.App ? "Desde la app del invitado." : null,
                },
            ],
            LineasServicio = (r.LineaServicioIds ?? []).Distinct().Select(id => new PedidoLineaServicio { LineaServicioId = id }).ToList(),
        };
        var parada = new Parada
        {
            RutaDia = rutaDia,
            Kind = ParadaKind.Recoleccion,
            Pedido = pedido,
            AddressId = address.Id,
            Turno = r.Turno,
            Notes = pedido.DriverNotes,
        };
        db.Pedidos.Add(pedido);
        db.Paradas.Add(parada);
        await db.SaveChangesAsync(ct);
        await handle.CommitAsync(ct);

        // The "cita agendada" notice (push or WhatsApp) arrives with notifications in week 6.
        var place = occupied + 1;
        return new ScheduleResultDto(true, pedido.Id, parada.Id, date, place,
            $"Recolección agendada el {AgendaCalculator.Format(date)}, lugar {place} de {cupo}.",
            availability);
    }

    // ---------------- entregas ----------------

    /// <summary>Etapas from which an entrega can be scheduled: in planta (partial entrega), ready, or already scheduled (change date).</summary>
    private static readonly PedidoEtapa[] EntregaEtapas = [PedidoEtapa.EnPlanta, PedidoEtapa.Listo, PedidoEtapa.EntregaConfirmada];

    /// <summary>Days with cupo for the entrega of a pedido, with the same rules as a recolección.</summary>
    public async Task<AvailabilityDto> EntregaAvailabilityAsync(int pedidoId, CancellationToken ct)
    {
        var pedido = await db.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pedidoId && p.Active, ct)
            ?? throw new NotFoundException($"El pedido {pedidoId} no existe.");
        var location = await LocateAsync(new AvailabilityRequest(pedido.AddressId, null, null, null, null, null), ct);
        return await CalculateAsync(location, clock.UtcNow, ct);
    }

    /// <summary>
    /// "Programar entrega" or "cambiar fecha" (Listo → EntregaConfirmada). Takes cupo like a recolección; a new date
    /// replaces the pending entrega parada.
    /// </summary>
    public async Task<EntregaResultDto> ScheduleEntregaAsync(int pedidoId, ScheduleEntregaRequest r, CancellationToken ct)
    {
        var pedido = await db.Pedidos.Include(p => p.LineasServicio).FirstOrDefaultAsync(p => p.Id == pedidoId && p.Active, ct)
            ?? throw new NotFoundException($"El pedido {pedidoId} no existe.");
        if (!EntregaEtapas.Contains(pedido.Etapa))
        {
            throw new DomainException($"El pedido está en {PedidoFlow.Name(pedido.Etapa)}; la entrega se programa cuando está en planta o listo.");
        }
        var location = await LocateAsync(new AvailabilityRequest(pedido.AddressId, null, null, null, null, null), ct);
        var result = await PlaceVisitAsync(pedido, ParadaKind.Entrega, location, clock.UtcNow, r.Date, ct);
        if (result.Scheduled)
        {
            var partial = pedido.Etapa == PedidoEtapa.EnPlanta;
            PedidoFlow.Advance(pedido, PedidoEtapa.EntregaConfirmada, clock.UtcNow, partial ? "Al programar la entrega parcial (lo que está listo)." : "Al programar la entrega.");
            await db.SaveChangesAsync(ct);
        }
        return result;
    }

    /// <summary>
    /// "No se entregó" (Pedidos › No se entregó): the attempt goes to the history, the entrega parada is marked as not
    /// done and the pedido is rescheduled to the next day with cupo of its zona, from tomorrow. There is no limit of
    /// attempts. The notice to the office and the invitado arrives with notifications (week 6).
    /// </summary>
    public async Task<EntregaResultDto> NoEntregadoAsync(int pedidoId, NoEntregadoRequest r, CancellationToken ct)
    {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId && p.Active, ct)
            ?? throw new NotFoundException($"El pedido {pedidoId} no existe.");
        if (pedido.Etapa is not (PedidoEtapa.EntregaConfirmada or PedidoEtapa.EnCamino))
        {
            throw new DomainException("Solo un pedido con entrega programada o en camino puede marcarse como no entregado.");
        }
        if (r.ReasonId is null && string.IsNullOrWhiteSpace(r.Note))
        {
            throw new DomainException("Indica el motivo por el que no se entregó.");
        }
        if (r.ReasonId is { } reasonId && !await db.Reasons.AnyAsync(x => x.Id == reasonId && x.Type == ReasonType.FailedEntrega && x.Active, ct))
        {
            throw new DomainException("El motivo no existe o no es de entregas fallidas.");
        }

        foreach (var parada in await db.Paradas.Where(p => p.PedidoId == pedidoId && p.Active && p.Kind == ParadaKind.Entrega
                     && p.State != ParadaState.Realizada && p.State != ParadaState.NoRealizada).ToListAsync(ct))
        {
            parada.State = ParadaState.NoRealizada;
        }
        pedido.FailedAttempts++;
        var from = pedido.Etapa;
        pedido.Etapa = PedidoEtapa.EntregaConfirmada;
        pedido.EtapaSince = clock.UtcNow;
        pedido.Eventos.Add(new PedidoEvento
        {
            Kind = PedidoEventoKind.NoEntregado,
            FromEtapa = from,
            ToEtapa = PedidoEtapa.EntregaConfirmada,
            Date = pedido.EntregaDate,
            ReasonId = r.ReasonId,
            Note = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim(),
        });
        await db.SaveChangesAsync(ct);

        // From tomorrow: registering at the start of tomorrow gives tomorrow as the minimum date.
        var location = await LocateAsync(new AvailabilityRequest(pedido.AddressId, null, null, null, null, null), ct);
        var result = await PlaceVisitAsync(pedido, ParadaKind.Entrega, location, BusinessTime.ToUtc(clock.Today.AddDays(1), TimeOnly.MinValue), null, ct);
        return result with { Message = $"Intento {pedido.FailedAttempts} sin entregar. {result.Message}" };
    }

    /// <summary>
    /// The chofer could not do the recolección (app del chofer › No realizada): it goes to the history, the parada is
    /// marked as not done and the recolección is rescheduled to the next day with cupo of its zona, from tomorrow.
    /// It is not counted as a failed entrega attempt.
    /// </summary>
    public async Task<EntregaResultDto> NoRecolectadoAsync(int pedidoId, int? reasonId, string? note, CancellationToken ct)
    {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId && p.Active, ct)
            ?? throw new NotFoundException($"El pedido {pedidoId} no existe.");
        if (pedido.Etapa != PedidoEtapa.Agendado)
        {
            throw new DomainException($"El pedido está en {PedidoFlow.Name(pedido.Etapa)}; solo una recolección agendada puede quedar no realizada.");
        }
        foreach (var parada in await db.Paradas.Where(p => p.PedidoId == pedidoId && p.Active && p.Kind == ParadaKind.Recoleccion
                     && p.State != ParadaState.Realizada && p.State != ParadaState.NoRealizada).ToListAsync(ct))
        {
            parada.State = ParadaState.NoRealizada;
        }
        pedido.Eventos.Add(new PedidoEvento
        {
            Kind = PedidoEventoKind.NoRecolectado,
            Date = pedido.RecoleccionDate,
            ReasonId = reasonId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        });
        await db.SaveChangesAsync(ct);

        var location = await LocateAsync(new AvailabilityRequest(pedido.AddressId, null, null, null, null, null), ct);
        return await PlaceVisitAsync(pedido, ParadaKind.Recoleccion, location, BusinessTime.ToUtc(clock.Today.AddDays(1), TimeOnly.MinValue), null, ct);
    }

    /// <summary>
    /// Days a recolección can move to from <paramref name="minimum"/> (the invitado's day change: Agenda.CorteCambioDia),
    /// with the zona's visit days, days off and cupo of the same engine.
    /// </summary>
    public async Task<AvailabilityDto> RecoleccionOptionsAsync(Pedido pedido, DateOnly minimum, CancellationToken ct)
    {
        var location = await LocateAsync(new AvailabilityRequest(pedido.AddressId, null, null, null, null, null), ct);
        return await CalculateAsync(location, BusinessTime.ToUtc(minimum, TimeOnly.MinValue), ct);
    }

    /// <summary>
    /// Moves a recolección (tracked <paramref name="pedido"/>) to <paramref name="date"/>, from <paramref name="minimum"/>:
    /// inside the agenda lock, replacing its pending parada; when the day filled up meanwhile, it answers the next one.
    /// </summary>
    public async Task<EntregaResultDto> RescheduleRecoleccionAsync(Pedido pedido, DateOnly minimum, DateOnly date, CancellationToken ct)
    {
        if (date == pedido.RecoleccionDate)
        {
            throw new DomainException($"Tu recolección ya es el {AgendaCalculator.Format(date)}.");
        }
        var location = await LocateAsync(new AvailabilityRequest(pedido.AddressId, null, null, null, null, null), ct);
        return await PlaceVisitAsync(pedido, ParadaKind.Recoleccion, location, BusinessTime.ToUtc(minimum, TimeOnly.MinValue), date, ct);
    }

    /// <summary>
    /// Creates the entrega (or rescheduled recolección) parada on the chosen (or first available) day, inside the
    /// agenda lock. It replaces the pending one of the same kind.
    /// </summary>
    private async Task<EntregaResultDto> PlaceVisitAsync(Pedido pedido, ParadaKind kind, ZonaLocation location, DateTimeOffset from, DateOnly? chosen, CancellationToken ct)
    {
        var availability = await CalculateAsync(location, from, ct);
        if (availability.OutOfCoverage)
        {
            throw new DomainException("La dirección está fuera de cobertura. Asigna la zona a mano en la dirección del invitado.");
        }
        var date = chosen ?? availability.Date
            ?? throw new DomainException($"No hay días con cupo en los siguientes {SearchDays} días para la zona {availability.ZonaCode}.");
        if (chosen is not null)
        {
            await EnsureDayCanBeChosenAsync(chosen.Value, location.Zona!, availability, ct);
        }

        var rutaId = availability.RutaId!.Value;
        var cupo = availability.Cupo!.Value;
        await using var handle = await agendaLock.AcquireAsync(rutaId, date, ct);
        var occupied = await OccupancyQuery(rutaId, date, date).CountAsync(ct)
            + (await fijas.PendingCupoAsync(rutaId, date, date, ct)).GetValueOrDefault((rutaId, date));
        if (occupied >= cupo)
        {
            return new EntregaResultDto(false, null, null,
                $"El {AgendaCalculator.Format(date)} ya no tiene cupo ({occupied} de {cupo}). Siguiente fecha disponible: "
                + (availability.Date is { } next && next != date ? $"{AgendaCalculator.Format(next)}." : "elige otro día."),
                availability);
        }

        // A new date replaces the pending visit.
        foreach (var old in await db.Paradas.Where(p => p.PedidoId == pedido.Id && p.Active && p.Kind == kind
                     && p.State == ParadaState.Pendiente).ToListAsync(ct))
        {
            old.Active = false;
        }
        var rutaDia = await db.RutaDias.FirstOrDefaultAsync(d => d.RutaId == rutaId && d.Date == date, ct);
        if (rutaDia is null)
        {
            rutaDia = new RutaDia { RutaId = rutaId, Date = date };
            db.RutaDias.Add(rutaDia);
        }
        db.Paradas.Add(new Parada
        {
            RutaDia = rutaDia,
            Kind = kind,
            PedidoId = pedido.Id,
            AddressId = pedido.AddressId,
            Turno = pedido.Turno,
            Notes = pedido.DriverNotes,
        });
        var entrega = kind == ParadaKind.Entrega;
        var previous = entrega ? pedido.EntregaDate : pedido.RecoleccionDate;
        if (entrega)
        {
            pedido.EntregaDate = date;
        }
        else
        {
            pedido.RecoleccionDate = date;
        }
        pedido.Eventos.Add(new PedidoEvento
        {
            Kind = entrega ? PedidoEventoKind.EntregaProgramada : PedidoEventoKind.RecoleccionProgramada,
            Date = date,
            Note = previous is { } p && p != date ? $"Antes: {AgendaCalculator.Format(p)}" : null,
        });
        await db.SaveChangesAsync(ct);
        await handle.CommitAsync(ct);

        var place = occupied + 1;
        return new EntregaResultDto(true, date, place,
            $"{(entrega ? "Entrega" : "Recolección")} el {AgendaCalculator.Format(date)}, lugar {place} de {cupo}.", availability);
    }

    private async Task<ZonaLocation> LocateAsync(AvailabilityRequest r, CancellationToken ct)
    {
        if (r.ZonaId is not null)
        {
            return await locator.ManualAsync(r.ZonaId.Value, ct);
        }
        if (r.AddressId is not null)
        {
            var d = await db.Addresses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.AddressId && x.Active, ct)
                ?? throw new NotFoundException($"La dirección {r.AddressId} no existe.");
            if (d.ZonaAssignedManually && d.ZonaId is not null)
            {
                return await locator.ManualAsync(d.ZonaId.Value, ct);
            }
            return await locator.LocateAsync(d.Location, d.PostalCode, d.Neighborhood, ct);
        }

        var point = r.Lat is not null && r.Lng is not null ? Geo.CreatePoint(r.Lat.Value, r.Lng.Value) : null;
        return await locator.LocateAsync(point, r.PostalCode, r.Neighborhood, ct);
    }

    private async Task<AvailabilityDto> CalculateAsync(ZonaLocation location, DateTimeOffset registeredAt, CancellationToken ct)
    {
        if (location.Zona is not { } zona)
        {
            return new AvailabilityDto(null, null, null, null, location.Source, true, null, null, null, null, [], [location.Explanation]);
        }

        var corte = await settings.GetTimeAsync(SettingKeys.AgendaCorteSameDay, ct);
        var cupo = await settings.GetIntegerAsync(SettingKeys.RutaCupoParadas, ct, zona.RutaId);

        // A registration time from a previous day never schedules in the past.
        var minimum = AgendaCutoffs.MinimumDate(registeredAt, corte);
        if (minimum < clock.Today)
        {
            minimum = clock.Today;
        }
        var until = minimum.AddDays(SearchDays);

        var nonWorking = await db.NonWorkingDays.AsNoTracking()
            .Where(d => d.Active && d.Date >= minimum && d.Date <= until)
            .Select(d => d.Date)
            .ToListAsync(ct);
        var occupancy = await OccupancyQuery(zona.RutaId, minimum, until)
            .GroupBy(p => p.RutaDia!.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, ct);
        // Paradas fijas take cupo even on days not generated yet.
        foreach (var ((_, date), count) in await fijas.PendingCupoAsync(zona.RutaId, minimum, until, ct))
        {
            occupancy[date] = occupancy.GetValueOrDefault(date) + count;
        }

        var result = AgendaCalculator.FindAvailability(new AvailabilityQuery(
            minimum, zona.EffectiveVisitDays(), nonWorking.ToHashSet(), occupancy, cupo, OtherDays, SearchDays));

        var registered = BusinessTime.TimeOfDay(registeredAt);
        var explanation = new List<string>
        {
            location.Explanation,
            $"Registro a las {registered:HH\\:mm}: {(registered < corte ? "antes" : "después")} del corte de las {corte:HH\\:mm}"
                + (registered < corte ? ", puede entrar el mismo día." : ", ya no entra el mismo día."),
        };
        explanation.AddRange(result.Explanation);

        return new AvailabilityDto(
            zona.Id, zona.Code, zona.RutaId, zona.Ruta?.Code, location.Source, false, minimum,
            result.Date, result.Place, result.Cupo,
            result.Days.Select(d => new AgendaDayDto(d.Date, AgendaCalculator.Format(d.Date), d.Occupied, d.Cupo, d.Available)).ToList(),
            explanation);
    }

    /// <summary>Active paradas of the ruta that take cupo in the range, generated paradas fijas included.</summary>
    private IQueryable<Parada> OccupancyQuery(int rutaId, DateOnly from, DateOnly to) =>
        db.Paradas.AsNoTracking().Where(p =>
            p.Active
            && p.RutaDia!.RutaId == rutaId
            && p.RutaDia.Date >= from && p.RutaDia.Date <= to
            && !Parada.KindsWithoutCupo.Contains(p.Kind));

    /// <summary>
    /// A chosen day must still follow the rules: from the minimum date, a visit day of the zona, not a day off.
    /// A full day is not an error here: the check inside the lock answers with the next available date.
    /// </summary>
    private async Task EnsureDayCanBeChosenAsync(DateOnly date, Zona zona, AvailabilityDto availability, CancellationToken ct)
    {
        if (availability.MinimumDate is { } minimum && date < minimum)
        {
            throw new DomainException($"El {AgendaCalculator.Format(date)} ya no se puede: la fecha mínima es el {AgendaCalculator.Format(minimum)}.");
        }
        if (!zona.EffectiveVisitDays().Contains(date.DayOfWeek))
        {
            throw new DomainException($"El {AgendaCalculator.Format(date)} no es día de visita de la zona {zona.Code}.");
        }
        if (await db.NonWorkingDays.AnyAsync(d => d.Active && d.Date == date, ct))
        {
            throw new DomainException($"El {AgendaCalculator.Format(date)} es día inhábil.");
        }
    }

    private async Task EnsureLineasServicioAsync(IReadOnlyList<int>? ids, CancellationToken ct)
    {
        if (ids is not { Count: > 0 })
        {
            return;
        }
        var wanted = ids.Distinct().ToList();
        var found = await db.LineasServicio.CountAsync(l => wanted.Contains(l.Id) && l.Active, ct);
        if (found != wanted.Count)
        {
            throw new DomainException("Alguna línea de servicio no existe o está inactiva.");
        }
    }
}
