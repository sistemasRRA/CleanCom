using CleanCom.Application.Access;
using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Common;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Pedidos;

/// <summary>
/// The Pedidos board (docs/pantallas/oficina/pedidos.md): each pedido from recolección to entrega, its history,
/// delays and moves by hand.
/// </summary>
public class PedidoBoardService(
    ICleanComDbContext db,
    IBusinessClock clock,
    ISettingService settings,
    IUserService users,
    IValidator<MoveEtapaRequest> moveValidator,
    IValidator<PedidoCommentRequest> commentValidator)
{
    /// <summary>Entregado shows the last days only.</summary>
    private const int DeliveredDays = 7;

    public async Task<PedidoBoardDto> BoardAsync(int? rutaId, string? q, bool onlyLate, CancellationToken ct)
    {
        var today = clock.Today;
        var deliveredSince = BusinessTime.ToUtc(today.AddDays(-DeliveredDays), TimeOnly.MinValue);
        var query = db.Pedidos.AsNoTracking().Where(p => p.Active && p.Etapa != PedidoEtapa.Cancelado
            && (p.Etapa != PedidoEtapa.Entregado || p.EtapaSince >= deliveredSince));
        if (!string.IsNullOrWhiteSpace(q))
        {
            var text = q.Trim();
            var digits = new string(text.Where(char.IsDigit).ToArray());
            int.TryParse(digits, out var folio);
            query = query.Where(p => p.Invitado!.Name.Contains(text)
                || (digits.Length > 0 && (p.Id == folio || p.Invitado.WhatsAppPhone.Contains(digits))));
        }

        var cards = await CardsAsync(query, ct);
        if (rutaId is not null)
        {
            cards = cards.Where(c => c.RutaId == rutaId).ToList();
        }
        var late = cards.Where(IsLateOrFailed).ToList();
        if (onlyLate)
        {
            cards = late;
        }

        var columns = PedidoFlow.Order.Select(etapa =>
        {
            var inColumn = cards.Where(c => c.Etapa == etapa)
                .OrderByDescending(c => c.DelayLevel)
                .ThenBy(c => c.Etapa >= PedidoEtapa.EntregaConfirmada ? c.EntregaDate : c.RecoleccionDate)
                .ThenBy(c => c.Id)
                .ToList();
            return new PedidoColumnDto(etapa, PedidoFlow.Name(etapa), inColumn.Count, inColumn);
        }).ToList();
        return new PedidoBoardDto(today, late.Count, columns);
    }

    public async Task<PedidoDetailDto> GetAsync(int id, CancellationToken ct)
    {
        var card = (await CardsAsync(db.Pedidos.AsNoTracking().Where(p => p.Id == id && p.Active), ct)).FirstOrDefault()
            ?? throw new NotFoundException($"El pedido {id} no existe.");
        var info = await db.Pedidos.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new
            {
                p.Address!.Street, p.Address.ExteriorNumber, p.Address.InteriorNumber, p.Address.Neighborhood,
                p.DriverNotes, p.Origin, p.RegisteredAt,
            })
            .FirstAsync(ct);

        var eventos = await db.PedidoEventos.AsNoTracking().Where(e => e.PedidoId == id)
            .OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id)
            .ToListAsync(ct);
        var reasonIds = eventos.Where(e => e.ReasonId is not null).Select(e => e.ReasonId!.Value).Distinct().ToList();
        var reasons = await db.Reasons.AsNoTracking().Where(r => reasonIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Text, ct);
        var names = await users.NamesAsync(eventos.Where(e => e.CreatedBy is not null).Select(e => e.CreatedBy!.Value).Distinct().ToList(), ct);
        var history = eventos.Select(e =>
        {
            var reason = e.ReasonId is { } rid ? reasons.GetValueOrDefault(rid) : null;
            return new PedidoEventoDto(e.Id, e.CreatedAt, e.CreatedBy is { } by ? names.GetValueOrDefault(by) : null,
                e.Kind, e.FromEtapa, e.ToEtapa, e.Date, e.Automatic, reason, e.Note, Describe(e, reason));
        }).ToList();

        var paradas = await db.Paradas.AsNoTracking().Where(p => p.PedidoId == id)
            .OrderBy(p => p.RutaDia!.Date).ThenBy(p => p.Id)
            .Select(p => new
            {
                p.Id, p.Kind, p.RutaDia!.Date, p.RutaDia.Ruta!.Code, p.State, p.ArrivedAt, p.Active, p.ClosedAt, p.GarmentsCollected, p.CloseNotes, p.Turno,
                Reason = p.NoRealizadaReasonId == null ? null : db.Reasons.Where(x => x.Id == p.NoRealizadaReasonId).Select(x => x.Text).FirstOrDefault(),
                Cobro = db.ReferencePayments.Where(c => c.ParadaId == p.Id && c.Active).Select(c => new { c.Method, c.Amount }).FirstOrDefault(),
                Evidence = p.Evidence.Where(e => e.Active).OrderBy(e => e.TakenAt)
                    .Select(e => new EvidenceRefDto(e.Id, e.Kind, e.TakenAt, e.DistanceMeters)).ToList(),
            })
            .ToListAsync(ct);

        var tickets = (await TicketQueries.ByPedidoAsync(db, settings, [id], clock.Today, ct)).GetValueOrDefault(id) ?? [];
        var address = info.Street + (info.ExteriorNumber is null ? "" : $" {info.ExteriorNumber}")
            + (info.InteriorNumber is null ? "" : $" int. {info.InteriorNumber}") + (info.Neighborhood is null ? "" : $", {info.Neighborhood}");
        return new PedidoDetailDto(card, address, info.DriverNotes, info.Origin, info.RegisteredAt, history, tickets,
            paradas.Select(p => new PedidoParadaDto(p.Id, p.Kind, p.Date, AgendaCalculator.Format(p.Date), p.Code, p.State, p.ArrivedAt, p.Active,
                p.ClosedAt, p.GarmentsCollected,
                string.Join(" · ", new[] { p.Reason, p.CloseNotes }.Where(x => !string.IsNullOrWhiteSpace(x))) is { Length: > 0 } note ? note : null,
                p.Cobro?.Method, p.Cobro?.Amount, p.Evidence, p.Turno)).ToList());
    }

    /// <summary>
    /// Moves the card by hand (the exception: etapas follow the chofer and planta on their own). Going back, delivering,
    /// cancelling needs a motivo. Cancelling takes its pending visits out of the rutas; a cancelled pedido is not reopened
    /// (a new one is scheduled).
    /// </summary>
    public async Task<PedidoDetailDto> MoveAsync(int id, MoveEtapaRequest r, CancellationToken ct)
    {
        await moveValidator.ValidateAndThrowAsync(r, ct);
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == id && p.Active, ct)
            ?? throw new NotFoundException($"El pedido {id} no existe.");
        if (pedido.Etapa == PedidoEtapa.Cancelado)
        {
            throw new DomainException("El pedido está cancelado; agenda uno nuevo.");
        }
        await ApplyMoveAsync(pedido, r.Etapa, r.Reason, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>
    /// Moves a tracked pedido by hand and, when cancelled, takes its pending visits out of the rutas (they free their
    /// cupo); delivered by hand, the pending entrega too. Does not save. Also used when the invitado cancels from the app.
    /// </summary>
    public async Task ApplyMoveAsync(Pedido pedido, PedidoEtapa to, string? reason, CancellationToken ct)
    {
        PedidoFlow.Move(pedido, to, reason, clock.UtcNow);
        if (to is PedidoEtapa.Cancelado or PedidoEtapa.Entregado)
        {
            foreach (var parada in await db.Paradas.Where(p => p.PedidoId == pedido.Id && p.Active && p.State == ParadaState.Pendiente
                         && (to == PedidoEtapa.Cancelado || p.Kind == ParadaKind.Entrega)).ToListAsync(ct))
            {
                parada.Active = false;
            }
        }
    }

    public async Task<PedidoDetailDto> CommentAsync(int id, PedidoCommentRequest r, CancellationToken ct)
    {
        await commentValidator.ValidateAndThrowAsync(r, ct);
        if (!await db.Pedidos.AnyAsync(p => p.Id == id && p.Active, ct))
        {
            throw new NotFoundException($"El pedido {id} no existe.");
        }
        db.PedidoEventos.Add(new PedidoEvento { PedidoId = id, Kind = PedidoEventoKind.Comentario, Note = r.Note.Trim() });
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    private static bool IsLateOrFailed(PedidoCardDto c) => c.DelayLevel == DelayLevel.Late || c.FailedAttempts > 0 && c.Etapa != PedidoEtapa.Entregado;

    /// <summary>Cards of the pedidos in the query (also used by Tickets en planta).</summary>
    public async Task<List<PedidoCardDto>> CardsAsync(IQueryable<Pedido> query, CancellationToken ct)
    {
        var rows = await query
            .Select(p => new
            {
                Pedido = p,
                InvitadoName = p.Invitado!.Name,
                Phone = p.Invitado.WhatsAppPhone,
                ZonaCode = p.Address!.Zona == null ? null : p.Address.Zona.Code,
                Lineas = p.LineasServicio.Select(l => l.LineaServicio!.Name).ToList(),
                ProcessingDays = p.LineasServicio.Max(l => l.LineaServicio!.ProcessingDays),
                // The ruta of its latest visit (the entrega once it has one).
                Ruta = db.Paradas.Where(x => x.PedidoId == p.Id && x.Active)
                    .OrderByDescending(x => x.RutaDia!.Date)
                    .Select(x => new { x.RutaDia!.RutaId, x.RutaDia.Ruta!.Code, x.RutaDia.Ruta.Color })
                    .FirstOrDefault(),
                CollectedAt = db.PedidoEventos.Where(e => e.PedidoId == p.Id && e.ToEtapa == PedidoEtapa.Recolectado)
                    .Min(e => (DateTimeOffset?)e.CreatedAt),
                TicketsTotal = db.Tickets.Count(t => t.PedidoId == p.Id && t.Active),
                TicketsReady = db.Tickets.Count(t => t.PedidoId == p.Id && t.Active && t.State == TicketState.Listo),
                PlantaAt = db.PedidoEventos.Where(e => e.PedidoId == p.Id && e.ToEtapa == PedidoEtapa.EnPlanta)
                    .Min(e => (DateTimeOffset?)e.CreatedAt),
            })
            .ToListAsync(ct);

        var today = clock.Today;
        var defaultDays = await settings.GetIntegerAsync(SettingKeys.PlantaProcessingDays, ct);
        return rows.Select(x =>
        {
            var p = x.Pedido;
            var plantaSince = x.PlantaAt is { } planta ? BusinessTime.Date(planta) : (DateOnly?)null;
            var delay = PedidoFlow.Delay(p, today, plantaSince, x.ProcessingDays ?? defaultDays, AgendaCalculator.Format);
            int? daysInStore = x.CollectedAt is { } collected && p.Etapa != PedidoEtapa.Entregado
                ? today.DayNumber - BusinessTime.Date(collected).DayNumber
                : null;
            return new PedidoCardDto(
                p.Id, p.Id.ToString(), x.Ruta?.RutaId, x.Ruta?.Code, x.Ruta?.Color, p.InvitadoId, x.InvitadoName, x.Phone, x.ZonaCode,
                p.Etapa, p.GarmentsApprox, x.Lineas, p.RecoleccionDate, p.EntregaDate, p.EtapaSince, daysInStore,
                delay.Level, delay.Text, p.FailedAttempts, p.Priority, x.TicketsTotal, x.TicketsReady);
        }).ToList();
    }

    private static string Describe(PedidoEvento e, string? reason)
    {
        var why = string.Join(" · ", new[] { reason, e.Note }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var tail = why.Length > 0 ? $": {why}" : "";
        return e.Kind switch
        {
            PedidoEventoKind.Etapa when e.FromEtapa is { } from && e.ToEtapa is { } to =>
                $"{PedidoFlow.Name(from)} → {PedidoFlow.Name(to)}{(e.Automatic ? " (automático)" : " (a mano)")}{tail}",
            PedidoEventoKind.Etapa when e.ToEtapa == PedidoEtapa.Agendado =>
                $"Agendado: recolección el {(e.Date is { } r ? AgendaCalculator.Format(r) : "—")}{tail}",
            PedidoEventoKind.EntregaProgramada => $"Entrega programada el {(e.Date is { } d ? AgendaCalculator.Format(d) : "—")}{tail}",
            PedidoEventoKind.NoEntregado => $"No se entregó{(e.Date is { } d2 ? $" el {AgendaCalculator.Format(d2)}" : "")}{tail}",
            PedidoEventoKind.Comentario => $"Comentario{tail}",
            PedidoEventoKind.Ticket => e.Note ?? "Ticket",
            PedidoEventoKind.AtrasoPlanta => $"Atraso en planta{tail}",
            PedidoEventoKind.NoRecolectado => $"No se recolectó{(e.Date is { } d3 ? $" el {AgendaCalculator.Format(d3)}" : "")}{tail}",
            PedidoEventoKind.RecoleccionProgramada => $"Recolección reprogramada el {(e.Date is { } d4 ? AgendaCalculator.Format(d4) : "—")}{tail}",
            _ => e.Kind.ToString(),
        };
    }
}
