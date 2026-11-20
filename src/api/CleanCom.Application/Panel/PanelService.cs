using CleanCom.Application.Agenda;
using CleanCom.Application.Common;
using CleanCom.Application.Fleet;
using CleanCom.Application.Operations;
using CleanCom.Application.Rutas;
using CleanCom.Application.Tracking;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Panel;

/// <summary>
/// Panel del día (docs/pantallas/oficina/panel-del-dia.md): indicators, pending tray, a card per ruta, the next 7 days'
/// load and the latest visits. Built from the same plan as Rutas and the same occupancy as the agenda.
/// </summary>
public class PanelService(
    ICleanComDbContext db,
    IBusinessClock clock,
    ISettingService settings,
    RutaDiaService rutas,
    AgendaService agenda,
    LocationService locations,
    UnidadService unidades)
{
    private const int LatestVisits = 8;
    private const int LoadDays = 7;

    public async Task<PanelDto> GetAsync(DateOnly? date, CancellationToken ct)
    {
        var today = clock.Today;
        var day = date ?? today;
        var isToday = day == today;
        var plans = await rutas.DayAsync(day, ct);
        var tolerance = await settings.GetIntegerAsync(SettingKeys.ParadaArrivalToleranceMinutes, ct);
        var noSignal = await settings.GetIntegerAsync(SettingKeys.GpsNoSignalMinutes, ct);
        var lastReadings = (await locations.LiveAsync(day, ct)).ToDictionary(l => l.RutaDiaId, l => l.At);

        var rutaDiaIds = plans.Rutas.Where(r => r.RutaDiaId is not null).Select(r => r.RutaDiaId!.Value).ToList();
        var collected = await db.ReferencePayments.AsNoTracking()
            .Where(c => c.Active)
            .Join(db.Paradas.Where(p => rutaDiaIds.Contains(p.RutaDiaId) && p.Active), c => c.ParadaId, p => p.Id, (c, p) => new { p.RutaDiaId, c.Amount })
            .GroupBy(x => x.RutaDiaId)
            .Select(g => new { RutaDiaId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.RutaDiaId, x => x.Amount, ct);
        var zonas = await db.Zonas.AsNoTracking().Where(z => z.Active).OrderBy(z => z.Code)
            .Select(z => new { z.RutaId, z.Code }).ToListAsync(ct);

        var cards = new List<PanelRutaDto>();
        var arrivals = 0;
        var onTime = 0;
        foreach (var plan in plans.Rutas)
        {
            var visits = plan.Paradas.Where(p => Visit(p.Kind)).ToList();
            var progress = PanelRules.Progress(visits.Select(p => p.State).ToList());
            foreach (var p in visits.Where(p => p.ArrivedAt is not null))
            {
                arrivals++;
                if (PanelRules.OnTime(BusinessTime.TimeOfDay(p.ArrivedAt!.Value), p.Arrival, tolerance))
                {
                    onTime++;
                }
            }

            var lastReading = plan.RutaDiaId is { } id && lastReadings.TryGetValue(id, out var at) ? at : (DateTimeOffset?)null;
            var next = visits.FirstOrDefault(p => p.State is ParadaState.Pendiente or ParadaState.EnCamino);
            var late = isToday ? PanelRules.LateMinutes(BusinessTime.TimeOfDay(clock.UtcNow), next?.Arrival, tolerance) : null;
            var alerts = new List<string>();
            if (late is { } minutes)
            {
                alerts.Add($"Atraso de {minutes} min: {next!.Name ?? "la siguiente parada"} estaba a las {next.Arrival:HH\\:mm}.");
            }
            if (plan.ReturnAt > plan.MaxReturn)
            {
                alerts.Add($"Horas extra: regreso estimado {plan.ReturnAt:HH\\:mm}, después de las {plan.MaxReturn:HH\\:mm}.");
            }
            if (isToday && PanelRules.WithoutGps(progress, lastReading, clock.UtcNow, noSignal))
            {
                alerts.Add(lastReading is null
                    ? "Sin GPS: el chofer no ha enviado su ubicación."
                    : $"Sin GPS desde las {BusinessTime.TimeOfDay(lastReading.Value):HH\\:mm}.");
            }
            if (plan.UnidadId is null && visits.Count > 0)
            {
                alerts.Add("Sin unidad asignada.");
            }
            alerts.AddRange(plan.Alerts.Where(a => !a.StartsWith("Horas extra", StringComparison.Ordinal)));

            cards.Add(new PanelRutaDto(
                plan.RutaId, plan.Code, plan.Name, plan.Color, plan.RutaDiaId,
                zonas.Where(z => z.RutaId == plan.RutaId).Select(z => z.Code).ToList(),
                plan.OperadorId, plan.OperadorName, plan.OperadorIsUsual, plan.UnidadId, plan.UnidadPlate,
                visits.Count(p => IsRecoleccion(p.Kind) && p.State == ParadaState.Realizada),
                visits.Count(p => IsRecoleccion(p.Kind)),
                await settings.GetIntegerAsync(SettingKeys.RutaRecoleccionGoal, ct, plan.RutaId),
                visits.Count(p => IsEntrega(p.Kind) && p.State == ParadaState.Realizada),
                visits.Count(p => IsEntrega(p.Kind)),
                visits.Count,
                visits.Count(p => PanelRules.IsClosed(p.State)),
                progress, plan.Km, plan.ReturnAt, plan.MaxReturn, lastReading, late,
                plan.RutaDiaId is { } rd ? collected.GetValueOrDefault(rd) : 0,
                alerts));
        }

        var indicators = new PanelIndicatorsDto(
            cards.Sum(c => c.Recolecciones), cards.Sum(c => c.RecoleccionesPlanned),
            cards.Sum(c => c.Entregas), cards.Sum(c => c.EntregasPlanned),
            cards.Sum(c => c.Collected),
            PanelRules.Percent(onTime, arrivals),
            await db.Pedidos.CountAsync(p => p.Active && (p.Etapa == PedidoEtapa.EnPlanta || p.Etapa == PedidoEtapa.Listo), ct));

        var board = await agenda.BoardAsync(today, LoadDays, ct);
        var fleet = await unidades.ListAsync(false, ct);
        return new PanelDto(
            day, AgendaCalculator.Format(day), isToday, plans.NonWorkingReason,
            await settings.GetTimeAsync(SettingKeys.RutaBaseDeparture, ct),
            await settings.GetTimeAsync(day.DayOfWeek == DayOfWeek.Saturday ? SettingKeys.RutaMaxReturnTimeSaturday : SettingKeys.RutaMaxReturnTime, ct),
            indicators,
            await PendingAsync(today, ct),
            fleet.WithPending,
            cards,
            new PanelLoadDto(board.Rutas, board.Days),
            await LatestVisitsAsync(day, ct),
            plans.BaseLat, plans.BaseLng);
    }

    /// <summary>The pending tray, always as of today: these are things to fix now, whatever day is on screen.</summary>
    private async Task<PanelPendingDto> PendingAsync(DateOnly today, CancellationToken ct)
    {
        var pastOpen = await db.Paradas.AsNoTracking()
            .Where(p => p.Active && p.RutaDia!.Date < today
                && p.State != ParadaState.Realizada && p.State != ParadaState.NoRealizada
                && p.Kind != ParadaKind.Salida && p.Kind != ParadaKind.Regreso && p.Kind != ParadaKind.Comida)
            .OrderBy(p => p.RutaDia!.Date).ThenBy(p => p.RutaDia!.Ruta!.Code)
            .Select(p => new PendingParadaDto(p.Id, p.PedidoId, p.RutaDia!.Date, p.RutaDia.Ruta!.Code,
                p.Pedido != null ? p.Pedido.Invitado!.Name : p.Title ?? (p.Lugar != null ? p.Lugar.Name : null), p.Kind, p.State))
            .ToListAsync(ct);

        var ready = await db.Pedidos.AsNoTracking()
            .Where(p => p.Active && p.Etapa == PedidoEtapa.Listo && p.EntregaDate == null)
            .OrderBy(p => p.EtapaSince)
            .Select(p => new { p.Id, p.Invitado!.Name, p.Etapa, p.EtapaSince })
            .ToListAsync(ct);

        var processingDays = await settings.GetIntegerAsync(SettingKeys.PlantaProcessingDays, ct);
        var limit = BusinessTime.ToUtc(today.AddDays(-processingDays).AddDays(1), TimeOnly.MinValue);
        var inPlanta = await db.Pedidos.AsNoTracking()
            .Where(p => p.Active && p.Etapa == PedidoEtapa.EnPlanta && p.EtapaSince < limit)
            .OrderBy(p => p.EtapaSince)
            .Select(p => new { p.Id, p.Invitado!.Name, p.Etapa, p.EtapaSince })
            .ToListAsync(ct);

        int Days(DateTimeOffset? since) => since is { } s ? today.DayNumber - BusinessTime.Date(s).DayNumber : 0;
        return new PanelPendingDto(
            pastOpen,
            ready.Select(p => new PendingPedidoDto(p.Id, p.Name, p.Etapa, p.EtapaSince, Days(p.EtapaSince))).ToList(),
            inPlanta.Select(p => new PendingPedidoDto(p.Id, p.Name, p.Etapa, p.EtapaSince, Days(p.EtapaSince))).ToList());
    }

    private async Task<IReadOnlyList<PanelVisitDto>> LatestVisitsAsync(DateOnly day, CancellationToken ct) =>
        await db.Paradas.AsNoTracking()
            .Where(p => p.Active && p.RutaDia!.Date == day && p.ClosedAt != null)
            .OrderByDescending(p => p.ClosedAt)
            .Take(LatestVisits)
            .Select(p => new PanelVisitDto(
                p.Id, p.PedidoId, p.RutaDia!.Ruta!.Code,
                p.Pedido != null ? p.Pedido.Invitado!.Name : p.Title ?? (p.Lugar != null ? p.Lugar.Name : null),
                p.Kind, p.State, p.ClosedAt!.Value,
                p.Evidence.Count(e => e.Active && e.Kind == EvidenceKind.Foto),
                p.Evidence.Any(e => e.Active && e.Kind == EvidenceKind.Firma)))
            .ToListAsync(ct);

    /// <summary>A visit to someone or somewhere; leaving, coming back and lunch are not.</summary>
    private static bool Visit(ParadaKind kind) => kind is not (ParadaKind.Salida or ParadaKind.Regreso or ParadaKind.Comida);

    private static bool IsRecoleccion(ParadaKind kind) => kind is ParadaKind.Recoleccion or ParadaKind.RecoleccionYEntrega;

    private static bool IsEntrega(ParadaKind kind) => kind is ParadaKind.Entrega or ParadaKind.RecoleccionYEntrega;
}
