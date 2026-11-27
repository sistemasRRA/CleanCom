using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Avisos;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Crm;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Crm;

/// <summary>
/// The ficha of the CRM beyond the follow-up (docs/pantallas/oficina/clientes-crm.md, Ficha: mejora): what the
/// invitado is worth, their readable history, duplicates and their app account.
/// </summary>
public class CrmInvitadoService(
    ICleanComDbContext db,
    IBusinessClock clock,
    ISettingService settings,
    IInvitadoAccounts accounts)
{
    /// <summary>Total spent, tickets, average ticket and services of an invitado.</summary>
    public async Task<CrmValueDto> ValueAsync(int id, CancellationToken ct)
    {
        var today = clock.Today;
        var amounts = await db.Tickets.AsNoTracking()
            .Where(t => t.Active && t.Amount != null && t.Pedido!.InvitadoId == id && t.Pedido.Active && t.Pedido.Etapa != PedidoEtapa.Cancelado)
            .Select(t => t.Amount!.Value).ToListAsync(ct);
        var services = await db.Pedidos.AsNoTracking()
            .Where(p => p.InvitadoId == id && p.Active && p.Etapa != PedidoEtapa.Cancelado && p.RecoleccionDate <= today)
            .Select(p => p.RecoleccionDate).ToListAsync(ct);
        var total = amounts.Sum();
        return new CrmValueDto(total, amounts.Count,
            amounts.Count == 0 ? null : Math.Round(total / amounts.Count, 2, MidpointRounding.AwayFromZero),
            services.Count, services.Count == 0 ? null : services.Min(), services.Count == 0 ? null : services.Max());
    }

    /// <summary>The app account of an invitado.</summary>
    public async Task<AppAccountDto> AccountAsync(Invitado invitado, CancellationToken ct)
    {
        if (invitado.UserId is not { } userId)
        {
            return new AppAccountDto(InvitadoAccount.State(false, false, invitado.InvitedToAppAt), null, invitado.InvitedToAppAt);
        }
        var info = (await accounts.InfoAsync([userId], ct)).GetValueOrDefault(userId);
        return new AppAccountDto(InvitadoAccount.State(info is not null, info?.Active ?? false, invitado.InvitedToAppAt), info?.LastAccess, invitado.InvitedToAppAt);
    }

    /// <summary>
    /// Pedidos, visits, tickets, avisos (WhatsApp and push) and contacts of an invitado in one list, newest first.
    /// </summary>
    public async Task<IReadOnlyList<HistoriaItemDto>> HistoryAsync(int id, CancellationToken ct)
    {
        await EnsureExistsAsync(id, ct);
        var items = new List<HistoriaItemDto>();

        var pedidos = await db.Pedidos.AsNoTracking().Where(p => p.InvitadoId == id && p.Active)
            .Select(p => new { p.Id, p.RegisteredAt, p.Origin, p.RecoleccionDate, p.Etapa, p.GarmentsApprox }).ToListAsync(ct);
        items.AddRange(pedidos.Select(p => new HistoriaItemDto(
            p.RegisteredAt, HistoriaKind.Pedido,
            $"Pedido {p.Id} agendado para el {AgendaFormat(p.RecoleccionDate)}{(p.Origin == PedidoOrigin.App ? " desde la app" : "")}",
            $"Etapa: {PedidoFlow.Name(p.Etapa)}{(p.GarmentsApprox is { } g ? $" · {g} prendas" : "")}", null, p.Id)));

        var visits = await db.Paradas.AsNoTracking()
            .Where(p => p.Active && p.ClosedAt != null && p.Pedido != null && p.Pedido.InvitadoId == id)
            .Select(p => new
            {
                p.PedidoId, p.Kind, p.State, ClosedAt = p.ClosedAt!.Value, p.GarmentsCollected, p.CloseNotes,
                Reason = db.Reasons.Where(r => r.Id == p.NoRealizadaReasonId).Select(r => r.Text).FirstOrDefault(),
                Payment = db.ReferencePayments.Where(r => r.ParadaId == p.Id && r.Active).Select(r => new { r.Method, r.Amount }).FirstOrDefault(),
            }).ToListAsync(ct);
        items.AddRange(visits.Select(v =>
        {
            var done = v.State == ParadaState.Realizada;
            var kind = v.Kind is ParadaKind.Entrega ? "Entrega" : v.Kind is ParadaKind.RecoleccionYEntrega ? "Entrega y recolección" : "Recolección";
            var text = $"{kind} {(done ? "realizada" : "no realizada")}{(done && v.GarmentsCollected is { } g ? $": {g} prendas" : "")}";
            var detail = done
                ? string.Join(" · ", new[] { v.Payment is { } p ? $"Cobro de referencia en {p.Method.ToString().ToLowerInvariant()}" : null, v.CloseNotes }.Where(s => s is not null))
                : string.Join(": ", new[] { v.Reason, v.CloseNotes }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return new HistoriaItemDto(v.ClosedAt, HistoriaKind.Visita, text, detail.Length == 0 ? null : detail, v.Payment?.Amount, v.PedidoId);
        }));

        var tickets = await db.Tickets.AsNoTracking().Where(t => t.Active && t.Pedido!.InvitadoId == id)
            .Select(t => new { t.CreatedAt, t.Folio, Linea = t.LineaServicio!.Name, t.Garments, t.Amount, t.PedidoId }).ToListAsync(ct);
        items.AddRange(tickets.Select(t => new HistoriaItemDto(
            t.CreatedAt, HistoriaKind.Ticket, $"Ticket {t.Folio} · {t.Linea}", $"{t.Garments} prendas", t.Amount, t.PedidoId)));

        var avisos = await db.Avisos.AsNoTracking().Where(a => a.InvitadoId == id)
            .Select(a => new { a.CreatedAt, a.SentAt, a.Canal, a.Text, a.State, a.PedidoId }).ToListAsync(ct);
        items.AddRange(avisos.Select(a => new HistoriaItemDto(
            a.SentAt ?? a.CreatedAt, HistoriaKind.Aviso,
            $"{(a.Canal == AvisoCanal.Push ? "Push" : "WhatsApp")}: {a.Text}", AvisoStateName(a.State), null, a.PedidoId)));

        var contacts = await db.Contacts.AsNoTracking().Where(c => c.InvitadoId == id && c.Active)
            .Select(c => new { c.ContactedAt, c.Channel, c.Result, c.Note }).ToListAsync(ct);
        items.AddRange(contacts.Select(c => new HistoriaItemDto(
            c.ContactedAt, HistoriaKind.Contacto,
            $"{(c.Channel == ContactChannel.Llamada ? "Llamada" : "WhatsApp")}: {ResultName(c.Result)}", c.Note, null, null)));

        return items.OrderByDescending(i => i.At).ToList();
    }

    /// <summary>Other active invitados who share a phone (WhatsApp or the other one) with this one.</summary>
    public async Task<IReadOnlyList<DuplicateDto>> DuplicatesAsync(int id, CancellationToken ct)
    {
        var invitado = await db.Invitados.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && i.Active, ct)
            ?? throw new NotFoundException($"El invitado {id} no existe.");
        var phones = InvitadoAccount.Phones(invitado.WhatsAppPhone, invitado.OtherPhone).ToList();
        if (phones.Count == 0)
        {
            return [];
        }
        // Phones are stored normalized, so equal digits find them; the domain rule confirms.
        var candidates = await db.Invitados.AsNoTracking()
            .Where(i => i.Active && i.Id != id && (phones.Contains(i.WhatsAppPhone) || (i.OtherPhone != null && phones.Contains(i.OtherPhone))))
            .ToListAsync(ct);
        var found = candidates.Where(c => InvitadoAccount.SharePhone(invitado, c)).ToList();
        var ids = found.Select(c => c.Id).ToList();
        var pedidos = await db.Pedidos.AsNoTracking().Where(p => ids.Contains(p.InvitadoId) && p.Active)
            .GroupBy(p => p.InvitadoId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, ct);
        return found.OrderBy(c => c.CreatedAt).Select(c => new DuplicateDto(
            c.Id, c.Name, c.WhatsAppPhone, c.OtherPhone, c.Email, pedidos.GetValueOrDefault(c.Id), c.UserId is not null, c.CreatedAt)).ToList();
    }

    /// <summary>
    /// Merges a duplicate into this invitado: their pedidos, addresses, contacts, beneficios, avisos and devices move
    /// here, this one takes the data it lacks (and the app account), and the duplicate gets a baja lógica. It cannot be
    /// undone.
    /// </summary>
    public async Task MergeAsync(int id, MergeRequest r, CancellationToken ct)
    {
        var main = await db.Invitados.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException($"El invitado {id} no existe.");
        var duplicate = await db.Invitados.FirstOrDefaultAsync(i => i.Id == r.DuplicadoId, ct)
            ?? throw new NotFoundException($"El invitado {r.DuplicadoId} no existe.");
        InvitadoAccount.EnsureCanMerge(main, duplicate);

        foreach (var p in await db.Pedidos.Where(x => x.InvitadoId == duplicate.Id).ToListAsync(ct)) p.InvitadoId = main.Id;
        foreach (var a in await db.Addresses.Where(x => x.InvitadoId == duplicate.Id).ToListAsync(ct)) a.InvitadoId = main.Id;
        foreach (var c in await db.Contacts.Where(x => x.InvitadoId == duplicate.Id).ToListAsync(ct)) c.InvitadoId = main.Id;
        foreach (var b in await db.Beneficios.Where(x => x.InvitadoId == duplicate.Id).ToListAsync(ct)) b.InvitadoId = main.Id;
        foreach (var a in await db.Avisos.Where(x => x.InvitadoId == duplicate.Id).ToListAsync(ct)) a.InvitadoId = main.Id;
        foreach (var a in await db.AvisosOficina.Where(x => x.InvitadoId == duplicate.Id).ToListAsync(ct)) a.InvitadoId = main.Id;
        foreach (var d in await db.DispositivosPush.Where(x => x.InvitadoId == duplicate.Id).ToListAsync(ct)) d.InvitadoId = main.Id;
        var userId = duplicate.UserId;
        InvitadoAccount.TakeMissing(main, duplicate);
        if (userId is not null && main.UserId == userId)
        {
            // The unique index on user_id: the duplicate frees it in a first save and the main invitado takes it in the
            // second. If the second failed, the next sign-in in the app links the account by its phone again.
            main.UserId = null;
            await db.SaveChangesAsync(ct);
            main.UserId = userId;
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// The invitation to the app for the office to send by WhatsApp (text of AppInvitado.MensajeInvitacion with the
    /// first name). Records when it was sent.
    /// </summary>
    public async Task<AppInvitationDto> InviteAsync(int id, CancellationToken ct)
    {
        var invitado = await db.Invitados.FirstOrDefaultAsync(i => i.Id == id && i.Active, ct)
            ?? throw new NotFoundException($"El invitado {id} no existe.");
        if (invitado.UserId is not null)
        {
            throw new DomainException("Ya tiene cuenta en la app.");
        }
        if (invitado.DoNotContact)
        {
            throw new DomainException("Pidió no ser contactado.");
        }
        var template = await settings.GetAsync(SettingKeys.AppInvitadoInvitation, null, ct);
        var text = template.Replace("{nombre}", AvisoRules.FirstName(invitado.Name), StringComparison.OrdinalIgnoreCase);
        invitado.InvitedToAppAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return new AppInvitationDto(text, $"https://wa.me/52{invitado.WhatsAppPhone}?text={Uri.EscapeDataString(text)}");
    }

    /// <summary>Blocks or gives back the access to the app; blocking ends the open sessions.</summary>
    public async Task SetBlockedAsync(int id, BlockAppRequest r, CancellationToken ct)
    {
        var invitado = await db.Invitados.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && i.Active, ct)
            ?? throw new NotFoundException($"El invitado {id} no existe.");
        var userId = invitado.UserId ?? throw new DomainException("No tiene cuenta en la app.");
        await accounts.SetActiveAsync(userId, !r.Blocked, ct);
    }

    private async Task EnsureExistsAsync(int id, CancellationToken ct)
    {
        if (!await db.Invitados.AnyAsync(i => i.Id == id && i.Active, ct))
        {
            throw new NotFoundException($"El invitado {id} no existe.");
        }
    }

    private static string AgendaFormat(DateOnly date) => Domain.Agenda.AgendaCalculator.Format(date);

    private static string AvisoStateName(AvisoState state) => state switch
    {
        AvisoState.Pendiente => "Por enviar",
        AvisoState.Enviado => "Enviado",
        AvisoState.Simulado => "Simulado (modo de prueba)",
        AvisoState.ParaMandarAMano => "Por mandar a mano",
        AvisoState.MandadoAMano => "Mandado a mano",
        AvisoState.Fallido => "Falló",
        _ => "Omitido",
    };

    private static string ResultName(ContactResult result) => result switch
    {
        ContactResult.NoContesto => "no contestó",
        ContactResult.LlamarDespues => "pidió que le llamaran después",
        ContactResult.NoPorAhora => "no por ahora",
        ContactResult.Agendo => "agendó",
        _ => "no contactar",
    };
}
