using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Pedidos;

/// <summary>
/// Tickets en planta (docs/pantallas/oficina/tickets-en-planta.md). Planta marks each ticket ready; the first ticket
/// moves the pedido to En planta and the last one ready moves it to Listo. Until the POS connection is defined, tickets
/// are captured by hand on this screen.
/// </summary>
public class PlantaService(
    ICleanComDbContext db,
    IBusinessClock clock,
    ISettingService settings,
    PedidoBoardService board,
    IValidator<CaptureTicketRequest> captureValidator,
    IValidator<PlantaCommentRequest> commentValidator)
{
    public async Task<PlantaBoardDto> BoardAsync(PlantaView view, CancellationToken ct)
    {
        var ids = await db.Tickets.AsNoTracking()
            .Where(t => t.Active && t.Pedido!.Active && t.Pedido.Etapa != PedidoEtapa.Entregado && t.Pedido.Etapa != PedidoEtapa.Cancelado)
            .Select(t => t.PedidoId).Distinct().ToListAsync(ct);
        var all = await CardsAsync(ids, ct);
        var threshold = await settings.GetIntegerAsync(SettingKeys.PlantaProcessingDays, ct);

        var indicators = new PlantaIndicatorsDto(
            all.Sum(c => c.TicketsTotal - c.TicketsReady),
            all.Count(c => c.TicketsReady == c.TicketsTotal),
            all.Count(c => c.TicketsReady > 0 && c.TicketsReady < c.TicketsTotal),
            all.Count(c => c.EntregaDueOrLate),
            all.Count(c => c.TicketsReady < c.TicketsTotal && c.DaysInPlanta >= threshold),
            threshold);

        // Urgency first: entrega today or late, then the longest in planta.
        var cards = all
            .Where(c => view == PlantaView.Listos ? c.TicketsReady == c.TicketsTotal : c.TicketsReady < c.TicketsTotal)
            .OrderByDescending(c => c.EntregaDueOrLate)
            .ThenByDescending(c => c.DaysInPlanta)
            .ThenBy(c => c.Pedido.Id)
            .ToList();
        return new PlantaBoardDto(clock.Today, view, indicators, cards);
    }

    /// <summary>Captures a ticket by hand; the first one of the pedido moves it to En planta.</summary>
    public async Task<PlantaCardDto> CaptureAsync(CaptureTicketRequest r, CancellationToken ct)
    {
        await captureValidator.ValidateAndThrowAsync(r, ct);
        var folio = Ticket.Normalize(r.Folio);
        if (await db.Tickets.Where(t => t.Active && t.Folio == folio).Select(t => (int?)t.PedidoId).FirstOrDefaultAsync(ct) is { } other)
        {
            throw new DomainException($"El folio {folio} ya está capturado en el pedido {other}.");
        }
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == r.PedidoId && p.Active, ct)
            ?? throw new DomainException($"El pedido {r.PedidoId} no existe.");
        var linea = await db.LineasServicio.AsNoTracking().FirstOrDefaultAsync(l => l.Id == r.LineaServicioId && l.Active, ct)
            ?? throw new DomainException("La línea de servicio no existe o está dada de baja.");

        var ticket = new Ticket
        {
            Folio = folio,
            PedidoId = pedido.Id,
            LineaServicioId = linea.Id,
            Garments = r.Garments,
            Amount = r.Amount,
            Source = TicketSource.Manual,
        };
        PlantaFlow.Receive(pedido, ticket, linea.Name, clock.UtcNow);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        return (await CardsAsync([pedido.Id], ct)).Single();
    }

    /// <summary>Scanned or typed folio: the ticket is ready. The last one of the pedido moves it to Listo.</summary>
    public async Task<TicketReadyResultDto> ReadyAsync(string folio, CancellationToken ct)
    {
        var normalized = Ticket.Normalize(folio ?? "");
        var ticket = await db.Tickets.Include(t => t.Pedido).FirstOrDefaultAsync(t => t.Active && t.Folio == normalized, ct)
            ?? throw new NotFoundException($"No existe el ticket {normalized}. Revisa el folio.");
        var pedido = ticket.Pedido!;
        var tickets = await db.Tickets.Where(t => t.PedidoId == pedido.Id && t.Active).ToListAsync(ct);
        var pedidoReady = PlantaFlow.MarkReady(ticket, pedido, tickets, clock.UtcNow);
        await db.SaveChangesAsync(ct);

        var card = (await CardsAsync([pedido.Id], ct)).Single();
        var missing = card.TicketsTotal - card.TicketsReady;
        var message = pedidoReady
            ? $"Pedido {pedido.Id} de {card.Pedido.InvitadoName} 100% listo ({card.TicketsTotal} ticket{(card.TicketsTotal == 1 ? "" : "s")})."
            : missing == 0
                ? $"Ticket {normalized} listo. El pedido {pedido.Id} está en {PedidoFlow.Name(pedido.Etapa)}."
                : $"Ticket {normalized} listo. Falta{(missing == 1 ? "" : "n")} {missing} del pedido {pedido.Id}.";
        return new TicketReadyResultDto(message, pedidoReady, card);
    }

    /// <summary>"¿Por qué se atrasa? Comentar": goes to the pedido history, visible in Pedidos.</summary>
    public async Task<PlantaCardDto> CommentAsync(PlantaCommentRequest r, CancellationToken ct)
    {
        await commentValidator.ValidateAndThrowAsync(r, ct);
        if (!await db.Pedidos.AnyAsync(p => p.Id == r.PedidoId && p.Active, ct))
        {
            throw new NotFoundException($"El pedido {r.PedidoId} no existe.");
        }
        if (r.ReasonId is { } reasonId && !await db.Reasons.AnyAsync(x => x.Id == reasonId && x.Type == ReasonType.PlantaDelay && x.Active, ct))
        {
            throw new DomainException("El motivo no existe o no es de atraso en planta.");
        }
        db.PedidoEventos.Add(new PedidoEvento
        {
            PedidoId = r.PedidoId,
            Kind = PedidoEventoKind.AtrasoPlanta,
            ReasonId = r.ReasonId,
            Note = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim(),
        });
        await db.SaveChangesAsync(ct);
        return (await CardsAsync([r.PedidoId], ct)).Single();
    }

    /// <summary>Baja lógica of a ticket captured by mistake (only while in process), with its motivo.</summary>
    public async Task<PlantaCardDto> DeactivateAsync(int ticketId, DeactivateTicketRequest r, CancellationToken ct)
    {
        var ticket = await db.Tickets.Include(t => t.Pedido).FirstOrDefaultAsync(t => t.Id == ticketId && t.Active, ct)
            ?? throw new NotFoundException($"El ticket {ticketId} no existe.");
        var pedido = ticket.Pedido!;
        var tickets = await db.Tickets.Where(t => t.PedidoId == pedido.Id && t.Active).ToListAsync(ct);
        PlantaFlow.Deactivate(ticket, pedido, tickets, r.Reason, clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return (await CardsAsync([pedido.Id], ct)).Single();
    }

    private async Task<List<PlantaCardDto>> CardsAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }
        var today = clock.Today;
        var pedidos = await board.CardsAsync(db.Pedidos.AsNoTracking().Where(p => ids.Contains(p.Id)), ct);
        var tickets = await TicketQueries.ByPedidoAsync(db, settings, ids, today, ct);
        var notes = await db.PedidoEventos.AsNoTracking()
            .Where(e => ids.Contains(e.PedidoId) && e.Kind == PedidoEventoKind.AtrasoPlanta)
            .OrderBy(e => e.CreatedAt).ThenBy(e => e.Id)
            .Select(e => new { e.PedidoId, Reason = e.ReasonId == null ? null : db.Reasons.Where(x => x.Id == e.ReasonId).Select(x => x.Text).FirstOrDefault(), e.Note })
            .ToListAsync(ct);
        var lastNote = notes.GroupBy(n => n.PedidoId)
            .ToDictionary(g => g.Key, g => g.Select(n => string.Join(" · ", new[] { n.Reason, n.Note }.Where(x => !string.IsNullOrWhiteSpace(x)))).Last());

        return pedidos.Select(p =>
        {
            var list = tickets.GetValueOrDefault(p.Id) ?? [];
            var since = list.Count > 0 ? BusinessTime.Date(list.Min(t => t.ReceivedAt)) : today;
            return new PlantaCardDto(
                p,
                today.DayNumber - since.DayNumber,
                p.EntregaDate is { } entrega && entrega <= today,
                list.Count(t => t.State == TicketState.Listo),
                list.Count,
                list,
                lastNote.GetValueOrDefault(p.Id));
        }).ToList();
    }
}

/// <summary>Tickets of several pedidos with their atraso, for Tickets en planta and the Pedidos card.</summary>
internal static class TicketQueries
{
    public static async Task<Dictionary<int, List<TicketDto>>> ByPedidoAsync(
        ICleanComDbContext db, ISettingService settings, IReadOnlyCollection<int> pedidoIds, DateOnly today, CancellationToken ct)
    {
        var defaultDays = await settings.GetIntegerAsync(SettingKeys.PlantaProcessingDays, ct);
        var rows = await db.Tickets.AsNoTracking()
            .Where(t => t.Active && pedidoIds.Contains(t.PedidoId))
            .OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
            .Select(t => new { Ticket = t, LineaName = t.LineaServicio!.Name, t.LineaServicio.ProcessingDays })
            .ToListAsync(ct);
        return rows.GroupBy(r => r.Ticket.PedidoId).ToDictionary(g => g.Key, g => g.Select(r =>
        {
            var t = r.Ticket;
            var delay = PlantaFlow.Delay(t, BusinessTime.Date(t.CreatedAt), today, r.ProcessingDays ?? defaultDays, AgendaCalculator.Format);
            return new TicketDto(t.Id, t.Folio, t.PedidoId, t.LineaServicioId, r.LineaName, t.Garments, t.Amount, t.State, t.Source,
                t.CreatedAt, t.ReadyAt, delay.Level, delay.Text);
        }).ToList());
    }
}
