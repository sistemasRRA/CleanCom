using CleanCom.Application.Common;
using CleanCom.Domain.Avisos;
using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Avisos;

/// <summary>
/// Avisos al invitado and to the office (docs/03-reglas-de-negocio.md, Avisos al invitado). Agenda, Pedidos and the
/// chofer app record here what happened; <see cref="AvisoDispatcher"/> sends it in the background. Recording never
/// fails the request that triggered it.
/// </summary>
public class AvisoService(ICleanComDbContext db, IBusinessClock clock, ICurrentUser currentUser, IEnumerable<IAvisoSender> senders)
{
    /// <summary>The same notice of the same pedido again within this time is a repeat (a double click, a resend) and is skipped.</summary>
    public static readonly TimeSpan RepeatWindow = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Records the notice of <paramref name="evento"/> for <paramref name="pedido"/> (tracked; it may be new). The
    /// caller saves. Nothing is recorded when the event's template is inactive or the invitado is a tienda interna.
    /// </summary>
    /// <param name="date">The date the notice tells (new recolección or entrega date); default the pedido's.</param>
    /// <param name="turno">The turno the notice tells; default the pedido's (recolección).</param>
    public async Task PedidoAsync(Pedido pedido, AvisoEvento evento, CancellationToken ct, DateOnly? date = null, Turno? turno = null)
    {
        var plantilla = await db.Plantillas.AsNoTracking().FirstOrDefaultAsync(p => p.Evento == evento, ct);
        var invitado = await db.Invitados.AsNoTracking().Where(i => i.Id == pedido.InvitadoId)
            .Select(i => new { i.Name, i.Type, i.ReceivesWhatsApp, i.WhatsAppPhone }).FirstOrDefaultAsync(ct);
        if (plantilla is not { Active: true } || invitado is null || !AvisoRules.IsNotified(invitado.Type))
        {
            return;
        }

        var first = AvisoRules.FirstName(invitado.Name);
        IReadOnlyList<string> values = evento switch
        {
            AvisoEvento.RecoleccionAgendada or AvisoEvento.CambioDeDia =>
                [first, AvisoRules.Date(date ?? pedido.RecoleccionDate), AvisoRules.Turno(turno ?? pedido.Turno, pedido.SpecificTime)],
            AvisoEvento.EntregaConfirmada =>
                [first, (date ?? pedido.EntregaDate) is { } entrega ? AvisoRules.Date(entrega) : "", AvisoRules.Turno(turno ?? Turno.CualquierHora, null)],
            AvisoEvento.PedidoCancelado => [first, pedido.Id.ToString()],
            AvisoEvento.RecoleccionRealizada => [first, AvisoRules.Garments(pedido.GarmentsApprox)],
            _ => [first],
        };
        var joined = string.Join(AvisoRules.ValueSeparator, values);
        if (pedido.Id != 0)
        {
            var since = clock.UtcNow - RepeatWindow;
            if (await db.Avisos.AnyAsync(a => a.PedidoId == pedido.Id && a.Evento == evento && a.Values == joined && a.CreatedAt >= since, ct))
            {
                return;
            }
        }

        var devices = await db.DispositivosPush.CountAsync(d => d.InvitadoId == pedido.InvitadoId && d.Active, ct);
        var canal = AvisoRules.Channel(devices, invitado.ReceivesWhatsApp, invitado.WhatsAppPhone);
        var push = canal == AvisoCanal.Push;
        db.Avisos.Add(new Aviso
        {
            InvitadoId = pedido.InvitadoId,
            Pedido = pedido,
            Evento = evento,
            Canal = canal,
            Phone = canal == AvisoCanal.WhatsApp ? invitado.WhatsAppPhone : null,
            MetaName = plantilla.MetaName,
            Values = joined,
            Title = push ? AvisoRules.Render(plantilla.PushTitle, values) : null,
            Text = AvisoRules.Render(push ? plantilla.PushText : plantilla.WhatsAppText, values),
            State = canal is null ? AvisoState.Omitido : AvisoState.Pendiente,
            Detail = canal is null ? AvisoRules.OmittedReason(invitado.ReceivesWhatsApp) : null,
        });
    }

    /// <summary>The notice of a change of etapa, when that etapa has one (<see cref="AvisoRules.ForEtapa"/>). The caller saves.</summary>
    public Task EtapaAsync(Pedido pedido, PedidoEtapa to, CancellationToken ct) =>
        AvisoRules.ForEtapa(to) is { } evento ? PedidoAsync(pedido, evento, ct) : Task.CompletedTask;

    /// <summary>A notice to the office (the bell) about what the invitado did in the app. The caller saves.</summary>
    public void Oficina(AvisoOficinaKind kind, Pedido pedido, string text) =>
        db.AvisosOficina.Add(new AvisoOficina { Kind = kind, InvitadoId = pedido.InvitadoId, PedidoId = pedido.Id == 0 ? null : pedido.Id, Text = text });

    // ---------------- reading and sending by hand ----------------

    public async Task<AvisosDto> ListAsync(AvisoState? state, int? pedidoId, CancellationToken ct)
    {
        var query = db.Avisos.AsNoTracking().AsQueryable();
        if (state is { } s)
        {
            query = query.Where(a => a.State == s);
        }
        if (pedidoId is { } p)
        {
            query = query.Where(a => a.PedidoId == p);
        }
        var avisos = await ToDtosAsync(query.OrderByDescending(a => a.Id).Take(200), ct, newestFirst: true);
        var toSend = await db.Avisos.CountAsync(a => a.State == AvisoState.ParaMandarAMano, ct);
        return new AvisosDto(avisos, toSend, Connected(AvisoCanal.WhatsApp), Connected(AvisoCanal.Push));
    }

    public async Task<IReadOnlyList<AvisoDto>> ForPedidoAsync(int pedidoId, CancellationToken ct) =>
        await ToDtosAsync(db.Avisos.AsNoTracking().Where(a => a.PedidoId == pedidoId), ct);

    /// <summary>"Ya lo mandé": the office sent a WhatsApp notice by hand.</summary>
    public async Task<AvisoDto> MarkSentByHandAsync(int id, CancellationToken ct)
    {
        var aviso = await db.Avisos.FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new NotFoundException("El aviso no existe.");
        if (aviso.State != AvisoState.ParaMandarAMano)
        {
            throw new DomainException("Este aviso no está pendiente de mandar a mano.");
        }
        aviso.State = AvisoState.MandadoAMano;
        aviso.Detail = null;
        aviso.SentAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return (await ToDtosAsync(db.Avisos.AsNoTracking().Where(a => a.Id == id), ct))[0];
    }

    // ---------------- office bell ----------------

    /// <summary>The notices not attended yet and the last attended ones (2 days).</summary>
    public async Task<AvisosOficinaDto> OficinaListAsync(CancellationToken ct)
    {
        var since = clock.UtcNow.AddDays(-2);
        var rows = await db.AvisosOficina.AsNoTracking()
            .Where(a => a.AttendedAt == null || a.AttendedAt >= since)
            .OrderByDescending(a => a.Id).Take(100)
            .Join(db.Invitados, a => a.InvitadoId, i => i.Id, (a, i) => new AvisoOficinaDto(a.Id, a.Kind, a.InvitadoId, i.Name, a.PedidoId, a.Text,
                a.CreatedAt, a.AttendedAt))
            .ToListAsync(ct);
        return new AvisosOficinaDto(rows.Count(a => a.AttendedAt is null), rows);
    }

    public async Task<AvisosOficinaDto> AttendAsync(int id, CancellationToken ct)
    {
        var aviso = await db.AvisosOficina.FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new NotFoundException("El aviso no existe.");
        if (aviso.AttendedAt is null)
        {
            aviso.AttendedAt = clock.UtcNow;
            aviso.AttendedBy = currentUser.Id;
            await db.SaveChangesAsync(ct);
        }
        return await OficinaListAsync(ct);
    }

    // ---------------- devices ----------------

    /// <summary>The invitado app registers its push token (again on every start). A token moved to another invitado leaves the old one.</summary>
    public async Task RegisterDeviceAsync(int invitadoId, RegisterDeviceRequest r, CancellationToken ct)
    {
        var token = r.Token.Trim();
        var device = await db.DispositivosPush.FirstOrDefaultAsync(d => d.Token == token, ct);
        if (device is null)
        {
            device = new DispositivoPush { Token = token };
            db.DispositivosPush.Add(device);
        }
        device.InvitadoId = invitadoId;
        device.Platform = r.Platform.Trim().ToLowerInvariant();
        device.LastSeenAt = clock.UtcNow;
        device.Active = true;
        await db.SaveChangesAsync(ct);
    }

    private bool Connected(AvisoCanal canal) => senders.FirstOrDefault(s => s.Canal == canal) is { TestMode: false };

    private async Task<List<AvisoDto>> ToDtosAsync(IQueryable<Aviso> query, CancellationToken ct, bool newestFirst = false)
    {
        var rows = await query.Join(db.Invitados, a => a.InvitadoId, i => i.Id, (a, i) => new { Aviso = a, i.Name }).ToListAsync(ct);
        var ordered = newestFirst ? rows.OrderByDescending(x => x.Aviso.Id) : rows.OrderBy(x => x.Aviso.Id);
        return ordered.Select(x =>
        {
            var a = x.Aviso;
            var link = a.Canal == AvisoCanal.WhatsApp && a.Phone is { } phone ? AvisoRules.WhatsAppLink(phone, a.Text) : null;
            return new AvisoDto(a.Id, a.Evento, AvisoRules.Name(a.Evento), a.Canal, a.State, a.InvitadoId, x.Name, a.Phone, a.PedidoId, a.Title,
                a.Text, a.Detail, link, a.CreatedAt, a.SentAt);
        }).ToList();
    }
}
