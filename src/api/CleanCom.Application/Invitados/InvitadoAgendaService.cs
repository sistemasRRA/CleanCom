using CleanCom.Application.Agenda;
using CleanCom.Application.Common;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Invitados;

/// <summary>
/// The invitado app on the single agenda engine (AgendaService, the same endpoints as Agendar) with the Invitado
/// role's rules (InvitadoScheduling), and the invitado's own pedidos.
/// </summary>
public class InvitadoAgendaService(ICleanComDbContext db, IBusinessClock clock, MeService me, AgendaService agenda)
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
            .OrderByDescending(p => p.Id).Select(p => (ParadaState?)p.State).FirstOrDefaultAsync(ct);
        return new MySeguimientoDto(ToDto(row), steps, visit);
    }

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
