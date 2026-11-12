using CleanCom.Application.Common;
using CleanCom.Application.Operations;
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

    public async Task<ScheduleResultDto> ScheduleAsync(ScheduleRequest r, PedidoOrigin origin, CancellationToken ct)
    {
        await scheduleValidator.ValidateAndThrowAsync(r, ct);
        if (origin == PedidoOrigin.App && (r.Priority || r.Turno == Turno.HoraEspecifica))
        {
            throw new DomainException("La hora específica y la prioridad solo las puede asignar oficina.");
        }
        var direccion = await db.Direcciones.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == r.DireccionId && d.InvitadoId == r.InvitadoId && d.Active, ct)
            ?? throw new NotFoundException("La dirección no existe o no es de este invitado.");
        await EnsureLineasServicioAsync(r.LineaServicioIds, ct);

        var registeredAt = r.RegisteredAt ?? clock.UtcNow;
        var location = await LocateAsync(new AvailabilityRequest(direccion.Id, null, null, null, null, null), ct);
        var availability = await CalculateAsync(location, registeredAt, ct);
        if (availability.OutOfCoverage)
        {
            throw new DomainException("La dirección está fuera de cobertura. Asigna la zona a mano en la dirección del invitado y vuelve a agendar.");
        }

        var date = r.Date ?? availability.Date
            ?? throw new DomainException($"No hay días con cupo en los siguientes {SearchDays} días para la zona {availability.ZonaCode}.");
        if (r.Date is not null)
        {
            EnsureDayCanBeChosen(r.Date.Value, availability);
        }

        var rutaId = availability.RutaId!.Value;
        var cupo = availability.Cupo!.Value;
        await using var handle = await agendaLock.AcquireAsync(rutaId, date, ct);

        // Same calculation again inside the lock: someone may have taken the place meanwhile.
        var occupied = await OccupancyQuery(rutaId, date, date).CountAsync(ct);
        if (occupied >= cupo)
        {
            var next = await CalculateAsync(location, registeredAt, ct);
            return new ScheduleResultDto(false, null, null, null, null,
                $"El {AgendaCalculator.Format(date)} se llenó mientras agendabas. Siguiente fecha disponible: "
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
            DireccionId = direccion.Id,
            Etapa = PedidoEtapa.Agendado,
            GarmentsApprox = r.GarmentsApprox,
            Turno = r.Turno,
            SpecificTime = r.SpecificTime,
            Priority = r.Priority,
            DriverNotes = string.IsNullOrWhiteSpace(r.DriverNotes) ? null : r.DriverNotes.Trim(),
            RegisteredAt = registeredAt,
            Origin = origin,
            RecoleccionDate = date,
            LineasServicio = (r.LineaServicioIds ?? []).Distinct().Select(id => new PedidoLineaServicio { LineaServicioId = id }).ToList(),
        };
        var parada = new Parada
        {
            RutaDia = rutaDia,
            Kind = ParadaKind.Recoleccion,
            Pedido = pedido,
            DireccionId = direccion.Id,
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

    private async Task<ZonaLocation> LocateAsync(AvailabilityRequest r, CancellationToken ct)
    {
        if (r.ZonaId is not null)
        {
            return await locator.ManualAsync(r.ZonaId.Value, ct);
        }
        if (r.DireccionId is not null)
        {
            var d = await db.Direcciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.DireccionId && x.Active, ct)
                ?? throw new NotFoundException($"La dirección {r.DireccionId} no existe.");
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
            .Where(d => d.Date >= minimum && d.Date <= until)
            .Select(d => d.Date)
            .ToListAsync(ct);
        var occupancy = await OccupancyQuery(zona.RutaId, minimum, until)
            .GroupBy(p => p.RutaDia!.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, ct);

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

    /// <summary>Paradas of the ruta that take cupo in the range, fixed paradas included.</summary>
    private IQueryable<Parada> OccupancyQuery(int rutaId, DateOnly from, DateOnly to) =>
        db.Paradas.AsNoTracking().Where(p =>
            p.RutaDia!.RutaId == rutaId
            && p.RutaDia.Date >= from && p.RutaDia.Date <= to
            && !Parada.KindsWithoutCupo.Contains(p.Kind));

    /// <summary>A day picked from the list must still follow the rules: from the minimum date, a visit day, not a day off.</summary>
    private static void EnsureDayCanBeChosen(DateOnly date, AvailabilityDto availability)
    {
        if (availability.MinimumDate is { } minimum && date < minimum)
        {
            throw new DomainException($"El {AgendaCalculator.Format(date)} ya no se puede: la fecha mínima es el {AgendaCalculator.Format(minimum)}.");
        }
        var day = availability.Days.FirstOrDefault(d => d.Date == date);
        if (day is null)
        {
            throw new DomainException($"El {AgendaCalculator.Format(date)} no es un día de visita disponible de la zona {availability.ZonaCode}.");
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
