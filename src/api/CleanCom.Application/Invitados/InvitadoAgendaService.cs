using CleanCom.Application.Agenda;
using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Application.Pedidos;
using CleanCom.Application.Tracking;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Invitados;

/// <summary>
/// The invitado app on the single agenda engine (AgendaService, the same endpoints as Agendar) with the Invitado
/// role's rules (InvitadoScheduling), and the invitado's own pedidos.
/// </summary>
public class InvitadoAgendaService(
    ICleanComDbContext db,
    IBusinessClock clock,
    MeService me,
    AgendaService agenda,
    LocationService locations,
    ISettingService settings,
    PedidoBoardService pedidos)
{
    private static readonly PedidoEtapa[] Path =
    [
        PedidoEtapa.Agendado, PedidoEtapa.Recolectado, PedidoEtapa.EnPlanta, PedidoEtapa.Listo,
        PedidoEtapa.EntregaConfirmada, PedidoEtapa.EnCamino, PedidoEtapa.Entregado,
    ];

    public async Task<AvailabilityDto> AvailabilityAsync(AvailabilityRequest r, CancellationToken ct)
    {
        InvitadoScheduling.EnsureAllowed(r.Turno, null, false, r.RegisteredAt, r.ZonaId);
        if (r.AddressId is { } addressId)
        {
            await EnsureOwnAddressAsync(await me.OwnIdAsync(ct), addressId, ct);
        }
        return await agenda.AvailabilityAsync(r, ct);
    }

    public async Task<ScheduleResultDto> ScheduleAsync(ScheduleRequest r, CancellationToken ct)
    {
        var own = await me.OwnIdAsync(ct);
        if (r.InvitadoId != own)
        {
            throw new NotFoundException("Solo puedes pedir servicio para ti.");
        }
        await EnsureOwnAddressAsync(own, r.AddressId, ct);
        InvitadoScheduling.EnsureAllowed(r.Turno, r.SpecificTime, r.Priority, r.RegisteredAt, null);
        return await agenda.ScheduleAsync(r, PedidoOrigin.App, ct);
    }

    /// <summary>The invitado's pedidos, newest first; cancelled ones stay so the invitado sees what happened.</summary>
    public async Task<IReadOnlyList<MyPedidoDto>> PedidosAsync(CancellationToken ct)
    {
        var own = await me.OwnIdAsync(ct);
        var rows = await Project(db.Pedidos.AsNoTracking().Where(p => p.Active && p.InvitadoId == own)
            .OrderByDescending(p => p.RecoleccionDate).ThenByDescending(p => p.Id)).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<MySeguimientoDto> SeguimientoAsync(int pedidoId, CancellationToken ct)
    {
        var own = await me.OwnIdAsync(ct);
        // Someone else's pedido answers as if it did not exist.
        var row = await Project(db.Pedidos.AsNoTracking().Where(p => p.Active && p.Id == pedidoId && p.InvitadoId == own)).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("El pedido no existe.");
        // Only the etapa changes: comments and reasons are for the office.
        var reached = await db.PedidoEventos.AsNoTracking()
            .Where(e => e.PedidoId == pedidoId && e.Kind == PedidoEventoKind.Etapa && e.ToEtapa != null)
            .GroupBy(e => e.ToEtapa!.Value)
            .Select(g => new { Etapa = g.Key, At = g.Max(e => e.CreatedAt) })
            .ToDictionaryAsync(x => x.Etapa, x => x.At, ct);
        var currentIndex = Array.IndexOf(Path, row.Etapa);
        var steps = Path.Select((etapa, i) => new MyPedidoStepDto(
            etapa, PedidoFlow.Name(etapa),
            etapa == PedidoEtapa.Agendado ? row.RegisteredAt : reached.TryGetValue(etapa, out var at) ? at : null,
            currentIndex >= 0 && i <= currentIndex, etapa == row.Etapa)).ToList();

        var today = clock.Today;
        var visit = await db.Paradas.AsNoTracking()
            .Where(p => p.PedidoId == pedidoId && p.Active && p.RutaDia!.Date == today)
            .OrderByDescending(p => p.Id)
            .Select(p => new
            {
                p.State,
                p.RutaDiaId,
                p.RutaDia!.RutaId,
                p.RutaDia.Ruta!.Code,
                p.RutaDia.Ruta.Color,
                // The chofer the office assigned that day, or the one whose usual ruta it is.
                Operador = p.RutaDia.Operador != null ? p.RutaDia.Operador.Name
                    : db.Operadores.Where(o => o.UsualRutaId == p.RutaDia.RutaId && o.Active).Select(o => o.Name).FirstOrDefault(),
                Destination = p.Address != null && p.Address.Location != null ? p.Address.Location
                    : p.Location != null ? p.Location
                    : p.Lugar != null ? p.Lugar.Location : null,
            })
            .FirstOrDefaultAsync(ct);
        // Change the day and cancel (docs/03-reglas-de-negocio.md, Cambios y cancelaciones desde la app del invitado).
        var recoleccion = await RecoleccionStateAsync(pedidoId, ct);
        var changeable = InvitadoScheduling.IsChangeable(row.Etapa, recoleccion);
        var corteCancelacion = await settings.GetTimeAsync(SettingKeys.AgendaCorteCancellation, ct);
        var canCancel = InvitadoScheduling.CanCancel(row.Etapa, recoleccion, clock.UtcNow, row.RecoleccionDate, corteCancelacion);
        var result = new MySeguimientoDto(ToDto(row), steps, visit?.State, null, changeable, canCancel,
            changeable ? AgendaCutoffs.CancellationDeadline(row.RecoleccionDate, corteCancelacion) : null);
        if (visit is null || !InvitadoTracking.ShowsChofer(visit.State))
        {
            return result;
        }

        var live = (await locations.LiveAsync(today, ct)).FirstOrDefault(l => l.RutaDiaId == visit.RutaDiaId);
        var destination = visit.Destination is { } d ? new GeoPoint(d.Y, d.X) : (GeoPoint?)null;
        DateTimeOffset? arrival = null;
        if (live is not null && destination is { } home)
        {
            arrival = InvitadoTracking.EstimatedArrival(new GeoPoint(live.Lat, live.Lng), live.At, home,
                (double)await settings.GetDecimalAsync(SettingKeys.RutaAverageSpeed, ct),
                (double)await settings.GetDecimalAsync(SettingKeys.RutaDistanceFactor, ct));
        }
        var chofer = new MyChoferDto(InvitadoTracking.FirstName(visit.Operador), visit.Code, visit.Color,
            live?.Lat, live?.Lng, live?.At, live?.SignalLostAt, destination?.Lat, destination?.Lng, arrival);
        return result with { Chofer = chofer };
    }

    /// <summary>
    /// Days the invitado can move the recolección to: from the next day before Agenda.CorteCambioDia, from two days
    /// later after it, with the zona's visit days, days off and cupo. The current day is left out.
    /// </summary>
    public async Task<AvailabilityDto> DayChangeOptionsAsync(int pedidoId, CancellationToken ct)
    {
        var pedido = await OwnPedidoAsync(pedidoId, ct);
        InvitadoScheduling.EnsureCanChangeDay(pedido.Etapa, await RecoleccionStateAsync(pedidoId, ct));
        var options = await agenda.RecoleccionOptionsAsync(pedido, await DayChangeMinimumAsync(ct), ct);
        return options with { Days = options.Days.Where(d => d.Date != pedido.RecoleccionDate).ToList() };
    }

    /// <summary>Moves the recolección to another day (same rules as <see cref="DayChangeOptionsAsync"/>).</summary>
    public async Task<EntregaResultDto> ChangeDayAsync(int pedidoId, ChangeDayRequest r, CancellationToken ct)
    {
        var pedido = await OwnPedidoAsync(pedidoId, ct);
        InvitadoScheduling.EnsureCanChangeDay(pedido.Etapa, await RecoleccionStateAsync(pedidoId, ct));
        return await agenda.RescheduleRecoleccionAsync(pedido, await DayChangeMinimumAsync(ct), r.Date, ct);
    }

    /// <summary>
    /// Cancels from the app up to Agenda.CorteCancelacion of the day before the recolección; afterwards only the office.
    /// Like a cancellation in Pedidos: it stays in the history and its visit leaves the ruta.
    /// </summary>
    public async Task<MySeguimientoDto> CancelAsync(int pedidoId, CancelPedidoRequest r, CancellationToken ct)
    {
        var pedido = await OwnPedidoAsync(pedidoId, ct);
        InvitadoScheduling.EnsureCanCancel(pedido.Etapa, await RecoleccionStateAsync(pedidoId, ct), clock.UtcNow, pedido.RecoleccionDate,
            await settings.GetTimeAsync(SettingKeys.AgendaCorteCancellation, ct));
        var reason = string.IsNullOrWhiteSpace(r.Note) ? "Cancelado por el invitado desde la app." : $"Cancelado por el invitado desde la app: {r.Note.Trim()}";
        await pedidos.ApplyMoveAsync(pedido, PedidoEtapa.Cancelado, reason, ct);
        await db.SaveChangesAsync(ct);
        return await SeguimientoAsync(pedidoId, ct);
    }

    /// <summary>The invitado's own pedido, tracked; someone else's answers as if it did not exist.</summary>
    private async Task<Pedido> OwnPedidoAsync(int pedidoId, CancellationToken ct)
    {
        var own = await me.OwnIdAsync(ct);
        return await db.Pedidos.FirstOrDefaultAsync(p => p.Active && p.Id == pedidoId && p.InvitadoId == own, ct)
            ?? throw new NotFoundException("El pedido no existe.");
    }

    /// <summary>State of the pedido's current recolección parada; null when it has none.</summary>
    private Task<ParadaState?> RecoleccionStateAsync(int pedidoId, CancellationToken ct) =>
        db.Paradas.AsNoTracking()
            .Where(p => p.PedidoId == pedidoId && p.Active && p.Kind != ParadaKind.Entrega)
            .OrderByDescending(p => p.Id).Select(p => (ParadaState?)p.State).FirstOrDefaultAsync(ct);

    private async Task<DateOnly> DayChangeMinimumAsync(CancellationToken ct) =>
        AgendaCutoffs.MinimumDateForDayChange(clock.UtcNow, await settings.GetTimeAsync(SettingKeys.AgendaCorteDayChange, ct));

    private async Task EnsureOwnAddressAsync(int invitadoId, int addressId, CancellationToken ct)
    {
        if (!await db.Addresses.AnyAsync(a => a.Id == addressId && a.InvitadoId == invitadoId && a.Active, ct))
        {
            throw new NotFoundException("La dirección no existe.");
        }
    }

    private sealed record Row(int Id, PedidoEtapa Etapa, DateOnly RecoleccionDate, DateOnly? EntregaDate, Turno Turno,
        List<string> Lineas, int? Garments, string? Label, string Street, string? Number, string? Neighborhood,
        PedidoOrigin Origin, DateTimeOffset RegisteredAt, int InvitadoId);

    /// <summary>Filter and order the pedidos first: EF cannot translate a filter on the projected record.</summary>
    private static IQueryable<Row> Project(IQueryable<Pedido> pedidos) =>
        pedidos.Select(p => new Row(
            p.Id, p.Etapa, p.RecoleccionDate, p.EntregaDate, p.Turno,
            p.LineasServicio.Select(l => l.LineaServicio!.Name).ToList(), p.GarmentsApprox,
            p.Address!.Label, p.Address.Street, p.Address.ExteriorNumber, p.Address.Neighborhood,
            p.Origin, p.RegisteredAt, p.InvitadoId));

    private static MyPedidoDto ToDto(Row p)
    {
        var street = string.Join(" ", new[] { p.Street, p.Number }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var line = string.Join(", ", new[] { street, p.Neighborhood }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return new MyPedidoDto(p.Id, p.Etapa, PedidoFlow.Name(p.Etapa), p.RecoleccionDate, p.EntregaDate, p.Turno, p.Lineas,
            p.Garments, string.IsNullOrWhiteSpace(p.Label) ? line : $"{p.Label} · {line}", p.Origin, p.RegisteredAt);
    }
}
