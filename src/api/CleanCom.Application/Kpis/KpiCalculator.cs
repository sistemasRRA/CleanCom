using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Application.Rutas;
using CleanCom.Domain.Avisos;
using CleanCom.Domain.Crm;
using CleanCom.Domain.Kpis;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Kpis;

/// <summary>
/// A piece of an indicator (a visit, a ticket, a pedido…) with what it adds to the value. The value and the detail of
/// an indicator come from the same rows, so what the detail lists is exactly what was counted.
/// </summary>
/// <param name="Key">The invitado, for distinct counts.</param>
public record KpiRow(
    DateOnly? Date,
    int? RutaId,
    int? PedidoId,
    int? InvitadoId,
    string? Name,
    string Text,
    decimal Numerator,
    decimal Denominator,
    int? Key = null)
{
    public KpiFact Fact => new(RutaId, Numerator, Denominator, Key);
}

/// <summary>
/// The rows of each indicator in a range of business dates (docs/03-reglas-de-negocio.md, KPIs). Visits count by the
/// day of their ruta; tickets, avisos and contacts by when they were registered (Mexico City time).
/// </summary>
public class KpiCalculator(ICleanComDbContext db, ISettingService settings, RutaDiaService rutas, IBusinessClock clock)
{
    public async Task<IReadOnlyList<KpiRow>> RowsAsync(string key, DateRange range, CancellationToken ct) => key switch
    {
        KpiCatalog.RecoleccionesPorRutaDia => await RecoleccionesAsync(range, ct),
        KpiCatalog.PrendasPorTicket => (await TicketsAsync(range, ct))
            .Where(t => t.Type == InvitadoType.ClienteFinal)
            .Select(t => t.Row($"Ticket {t.Folio}: {t.Garments} prenda{(t.Garments == 1 ? "" : "s")}", t.Garments, 1)).ToList(),
        KpiCatalog.CobroAnticipado => await CobroAnticipadoAsync(range, ct),
        KpiCatalog.ClientesNuevos => await ClientesNuevosAsync(range, ct),
        KpiCatalog.ClientesAtendidos => (await VisitsAsync(range, ct))
            .Where(v => v.State == ParadaState.Realizada && v.InvitadoId is not null)
            .GroupBy(v => (v.InvitadoId, v.RutaId))
            .Select(g => new KpiRow(g.Max(v => v.Date), g.Key.RutaId, g.Last().PedidoId, g.Key.InvitadoId, g.First().Name,
                g.Count() == 1 ? "1 visita realizada" : $"{g.Count()} visitas realizadas", 1, 0, g.Key.InvitadoId))
            .ToList(),
        KpiCatalog.TicketsFirmados => (await VisitsAsync(range, ct))
            .Where(v => v.State == ParadaState.Realizada && v.PedidoId is not null && IsEntrega(v.Kind))
            .Select(v => v.Row(v.HasSignature ? "Entrega con firma" : "Entrega sin firma", v.HasSignature ? 1 : 0, 1)).ToList(),
        KpiCatalog.VisitasConFoto => (await VisitsAsync(range, ct))
            .Where(v => v.State == ParadaState.Realizada)
            .Select(v => v.Row(v.HasPhoto ? "Con foto" : "Sin foto", v.HasPhoto ? 1 : 0, 1)).ToList(),
        KpiCatalog.PedidosFueraDeTiempo => await FueraDeTiempoAsync(range, ct),
        KpiCatalog.LlegadasATiempo => await LlegadasAsync(range, ct),
        KpiCatalog.Nps or KpiCatalog.EncuestasRespondidas => [],
        KpiCatalog.ToquesWhatsApp => await ToquesAsync(range, ct),
        KpiCatalog.FueraDeCobertura => await FueraDeCoberturaAsync(range, ct),
        KpiCatalog.TicketPromedio => (await TicketsAsync(range, ct))
            .Where(t => t.Amount is not null)
            .Select(t => t.Row($"Ticket {t.Folio}: {Money(t.Amount!.Value)}", t.Amount!.Value, 1)).ToList(),
        KpiCatalog.FacturacionSinConvenios => (await TicketsAsync(range, ct))
            .Where(t => t.Amount is not null && t.Type == InvitadoType.ClienteFinal)
            .Select(t => t.Row($"Ticket {t.Folio}: {Money(t.Amount!.Value)}", t.Amount!.Value, 0)).ToList(),
        KpiCatalog.Recurrencia => await ServiciosAsync(range, ct),
        _ => throw new NotFoundException($"El indicador {key} no existe."),
    };

    /// <summary>Recolecciones realizadas (numerator) and each day a ruta closed some visit (denominator).</summary>
    private async Task<IReadOnlyList<KpiRow>> RecoleccionesAsync(DateRange range, CancellationToken ct)
    {
        var visits = await VisitsAsync(range, ct);
        var done = visits.Where(v => v.State == ParadaState.Realizada && IsRecoleccion(v.Kind))
            .Select(v => v.Row("Recolección realizada", 1, 0));
        var days = visits.Where(v => PanelRules.IsClosed(v.State))
            .Select(v => (v.Date, v.RutaId)).Distinct()
            .Select(d => new KpiRow(d.Date, d.RutaId, null, null, null, "Día operado", 0, 1));
        return [.. days.Concat(done).OrderBy(r => r.Date)];
    }

    /// <summary>Pedidos delivered in the range; paid in advance when the chofer registered a charge at its recolección.</summary>
    private async Task<IReadOnlyList<KpiRow>> CobroAnticipadoAsync(DateRange range, CancellationToken ct)
    {
        var entregas = Entregas(await VisitsAsync(range, ct));
        var pedidoIds = entregas.Select(v => v.PedidoId!.Value).Distinct().ToList();
        var paid = (await db.ReferencePayments.AsNoTracking()
            .Where(c => c.Active && c.Amount > 0)
            .Join(db.Paradas.Where(p => p.Active && p.PedidoId != null && pedidoIds.Contains(p.PedidoId.Value) && p.Kind == ParadaKind.Recoleccion),
                c => c.ParadaId, p => p.Id, (c, p) => p.PedidoId!.Value)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();
        return entregas.Select(v => paid.Contains(v.PedidoId!.Value)
            ? v.Row("Cobrado en la recolección", 1, 1)
            : v.Row("Cobrado en la entrega o sin cobro registrado", 0, 1)).ToList();
    }

    /// <summary>
    /// Invitados whose first service fell in the range, with the same service dates as the CRM (recolección of each
    /// pedido not cancelled, up to today).
    /// </summary>
    private async Task<IReadOnlyList<KpiRow>> ClientesNuevosAsync(DateRange range, CancellationToken ct)
    {
        var today = clock.Today;
        var firsts = await db.Pedidos.AsNoTracking()
            .Where(p => p.Active && p.Etapa != PedidoEtapa.Cancelado && p.RecoleccionDate <= today)
            .GroupBy(p => p.InvitadoId)
            .Select(g => new { InvitadoId = g.Key, First = g.Min(p => p.RecoleccionDate) })
            .Where(x => x.First >= range.Start && x.First <= range.End)
            .ToListAsync(ct);
        if (firsts.Count == 0)
        {
            return [];
        }
        var ids = firsts.Select(f => f.InvitadoId).ToList();
        var pedidos = await ServicePedidos(new DateRange(range.Start, range.End < today ? range.End : today), ids).ToListAsync(ct);
        return firsts
            .Select(f => pedidos.Where(p => p.InvitadoId == f.InvitadoId && p.Date == f.First).OrderBy(p => p.PedidoId).FirstOrDefault())
            .OfType<ServicePedido>()
            .Select(p => new KpiRow(p.Date, p.RutaId, p.PedidoId, p.InvitadoId, p.Name, "Primer servicio", 1, 0, p.InvitadoId))
            .OrderBy(r => r.Date)
            .ToList();
    }

    /// <summary>Services (pedidos not cancelled) over the different invitados who had them.</summary>
    private async Task<IReadOnlyList<KpiRow>> ServiciosAsync(DateRange range, CancellationToken ct)
    {
        var today = clock.Today;
        var upTo = new DateRange(range.Start, range.End < today ? range.End : today);
        if (upTo.End < upTo.Start)
        {
            return [];
        }
        return (await ServicePedidos(upTo).ToListAsync(ct))
            .Select(p => new KpiRow(p.Date, p.RutaId, p.PedidoId, p.InvitadoId, p.Name, "Servicio", 1, 0, p.InvitadoId))
            .ToList();
    }

    /// <summary>Delivered after the first entrega date the pedido was given (rescheduling does not move it).</summary>
    private async Task<IReadOnlyList<KpiRow>> FueraDeTiempoAsync(DateRange range, CancellationToken ct)
    {
        var entregas = Entregas(await VisitsAsync(range, ct));
        var pedidoIds = entregas.Select(v => v.PedidoId!.Value).Distinct().ToList();
        var firstPromised = await db.PedidoEventos.AsNoTracking()
            .Where(e => pedidoIds.Contains(e.PedidoId) && e.Kind == PedidoEventoKind.EntregaProgramada && e.Date != null)
            .GroupBy(e => e.PedidoId)
            .Select(g => new { PedidoId = g.Key, Date = g.OrderBy(e => e.CreatedAt).ThenBy(e => e.Id).Select(e => e.Date).First() })
            .ToDictionaryAsync(x => x.PedidoId, x => x.Date!.Value, ct);
        var current = await db.Pedidos.AsNoTracking().Where(p => pedidoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.EntregaDate, ct);

        var rows = new List<KpiRow>();
        foreach (var v in entregas)
        {
            var promised = firstPromised.TryGetValue(v.PedidoId!.Value, out var first) ? first : current.GetValueOrDefault(v.PedidoId.Value);
            if (promised is { } p && v.Date > p)
            {
                var late = v.Date.DayNumber - p.DayNumber;
                rows.Add(v.Row($"Prometido el {p:dd/MM}, entregado {late} día{(late == 1 ? "" : "s")} después", 1, 0));
            }
        }
        return rows;
    }

    /// <summary>Each "Llegué" against the estimated arrival of the plan of that day.</summary>
    private async Task<IReadOnlyList<KpiRow>> LlegadasAsync(DateRange range, CancellationToken ct)
    {
        var arrived = (await VisitsAsync(range, ct)).Where(v => v.ArrivedAt is not null).ToList();
        var tolerance = await settings.GetIntegerAsync(SettingKeys.ParadaArrivalToleranceMinutes, ct);
        var rows = new List<KpiRow>();
        foreach (var day in arrived.Select(v => v.Date).Distinct().Order())
        {
            var estimated = (await rutas.DayAsync(day, ct)).Rutas.SelectMany(r => r.Paradas).ToDictionary(p => p.Id, p => p.Arrival);
            foreach (var v in arrived.Where(v => v.Date == day))
            {
                if (!estimated.TryGetValue(v.Id, out var plan))
                {
                    continue;
                }
                var real = BusinessTime.TimeOfDay(v.ArrivedAt!.Value);
                var onTime = PanelRules.OnTime(real, plan, tolerance);
                rows.Add(v.Row($"Llegó {real:HH\\:mm}, estimada {plan:HH\\:mm}{(onTime ? "" : " (fuera de tolerancia)")}", onTime ? 1 : 0, 1));
            }
        }
        return rows;
    }

    /// <summary>WhatsApp avisos that went out (or were sent by hand) and CRM contacts by WhatsApp.</summary>
    private async Task<IReadOnlyList<KpiRow>> ToquesAsync(DateRange range, CancellationToken ct)
    {
        var (from, to) = Utc(range);
        var avisos = await db.Avisos.AsNoTracking()
            .Where(a => a.Canal == AvisoCanal.WhatsApp && (a.State == AvisoState.Enviado || a.State == AvisoState.MandadoAMano)
                && a.SentAt >= from && a.SentAt < to)
            .Join(db.Invitados, a => a.InvitadoId, i => i.Id, (a, i) => new
            {
                a.SentAt,
                a.Evento,
                a.PedidoId,
                a.InvitadoId,
                i.Name,
                RutaId = a.Pedido != null && a.Pedido.Address!.Zona != null ? (int?)a.Pedido.Address.Zona.RutaId : null,
            })
            .ToListAsync(ct);
        var contacts = await db.Contacts.AsNoTracking()
            .Where(c => c.Active && c.Channel == ContactChannel.WhatsApp && c.ContactedAt >= from && c.ContactedAt < to)
            .Select(c => new { c.ContactedAt, c.InvitadoId, c.Invitado!.Name, c.Result })
            .ToListAsync(ct);
        return avisos.Select(a => new KpiRow(BusinessTime.Date(a.SentAt!.Value), a.RutaId, a.PedidoId, a.InvitadoId, a.Name, $"Aviso: {AvisoText(a.Evento)}", 1, 0))
            .Concat(contacts.Select(c => new KpiRow(BusinessTime.Date(c.ContactedAt), null, null, c.InvitadoId, c.Name, $"Contacto del CRM: {ContactText(c.Result)}", 1, 0)))
            .OrderBy(r => r.Date)
            .ToList();
    }

    /// <summary>Visits done at an address outside every active zona polygon (the office chose its zona by hand).</summary>
    private async Task<IReadOnlyList<KpiRow>> FueraDeCoberturaAsync(DateRange range, CancellationToken ct)
    {
        var outside = (await Visits(range)
            .Where(p => p.State == ParadaState.Realizada && p.Address != null && p.Address.Location != null
                && !db.Zonas.Any(z => z.Active && z.Polygon != null && z.Polygon.Contains(p.Address.Location)))
            .Select(p => p.Id)
            .ToListAsync(ct)).ToHashSet();
        return (await VisitsAsync(range, ct)).Where(v => outside.Contains(v.Id))
            .Select(v => v.Row("Dirección fuera de los polígonos", 1, 0)).ToList();
    }

    private IQueryable<Parada> Visits(DateRange range) =>
        db.Paradas.AsNoTracking().Where(p => p.Active && p.RutaDia!.Date >= range.Start && p.RutaDia.Date <= range.End
            && p.Kind != ParadaKind.Salida && p.Kind != ParadaKind.Regreso && p.Kind != ParadaKind.Comida);

    /// <summary>Every visit of the range (leaving, coming back and lunch are not visits).</summary>
    private Task<List<Visit>> VisitsAsync(DateRange range, CancellationToken ct) =>
        Visits(range)
            .OrderBy(p => p.RutaDia!.Date).ThenBy(p => p.RutaDia!.RutaId).ThenBy(p => p.Position)
            .Select(p => new Visit(
                p.Id,
                p.RutaDia!.Date,
                p.RutaDia.RutaId,
                p.Kind,
                p.State,
                p.PedidoId,
                p.Pedido != null ? (int?)p.Pedido.InvitadoId : null,
                p.Pedido != null ? p.Pedido.Invitado!.Name : p.Title ?? (p.Lugar != null ? p.Lugar.Name : null),
                p.ArrivedAt,
                p.Evidence.Any(e => e.Active && e.Kind == EvidenceKind.Foto),
                p.Evidence.Any(e => e.Active && e.Kind == EvidenceKind.Firma)))
            .ToListAsync(ct);

    private static List<Visit> Entregas(IEnumerable<Visit> visits) =>
        visits.Where(v => v.State == ParadaState.Realizada && v.PedidoId is not null && IsEntrega(v.Kind)).ToList();

    private async Task<List<TicketData>> TicketsAsync(DateRange range, CancellationToken ct)
    {
        var (from, to) = Utc(range);
        var tickets = await db.Tickets.AsNoTracking()
            .Where(t => t.Active && t.Pedido!.Active && t.CreatedAt >= from && t.CreatedAt < to)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new
            {
                t.CreatedAt,
                t.Folio,
                t.Garments,
                t.Amount,
                t.PedidoId,
                t.Pedido!.InvitadoId,
                t.Pedido.Invitado!.Name,
                t.Pedido.Invitado.Type,
                RutaId = t.Pedido.Address!.Zona != null ? (int?)t.Pedido.Address.Zona.RutaId : null,
            })
            .ToListAsync(ct);
        return tickets.Select(t => new TicketData(BusinessTime.Date(t.CreatedAt), t.RutaId, t.PedidoId, t.InvitadoId, t.Name, t.Type, t.Folio, t.Garments, t.Amount)).ToList();
    }

    /// <summary>Pedidos not cancelled collected in the range, optionally only of some invitados (filtered before projecting).</summary>
    private IQueryable<ServicePedido> ServicePedidos(DateRange range, List<int>? invitadoIds = null) =>
        db.Pedidos.AsNoTracking()
            .Where(p => p.Active && p.Etapa != PedidoEtapa.Cancelado && p.RecoleccionDate >= range.Start && p.RecoleccionDate <= range.End)
            .Where(p => invitadoIds == null || invitadoIds.Contains(p.InvitadoId))
            .OrderBy(p => p.RecoleccionDate).ThenBy(p => p.Id)
            .Select(p => new ServicePedido(p.RecoleccionDate, p.Address!.Zona != null ? (int?)p.Address.Zona.RutaId : null, p.Id, p.InvitadoId, p.Invitado!.Name));

    private static (DateTimeOffset From, DateTimeOffset To) Utc(DateRange range) =>
        (BusinessTime.ToUtc(range.Start, TimeOnly.MinValue), BusinessTime.ToUtc(range.End.AddDays(1), TimeOnly.MinValue));

    private static readonly System.Globalization.CultureInfo Mexico = new("es-MX");

    private static string Money(decimal amount) => amount.ToString("C", Mexico);

    private static bool IsRecoleccion(ParadaKind kind) => kind is ParadaKind.Recoleccion or ParadaKind.RecoleccionYEntrega;

    private static bool IsEntrega(ParadaKind kind) => kind is ParadaKind.Entrega or ParadaKind.RecoleccionYEntrega;

    private static string AvisoText(AvisoEvento evento) => evento switch
    {
        AvisoEvento.RecoleccionAgendada => "recolección agendada",
        AvisoEvento.CambioDeDia => "cambio de día",
        AvisoEvento.PedidoCancelado => "pedido cancelado",
        AvisoEvento.ChoferEnCamino => "chofer en camino",
        AvisoEvento.RecoleccionRealizada => "recolección realizada",
        AvisoEvento.PrendasEnProceso => "prendas en proceso",
        AvisoEvento.EntregaConfirmada => "entrega confirmada",
        AvisoEvento.EntregaEnCamino => "entrega en camino",
        AvisoEvento.Entregado => "entregado",
        _ => evento.ToString(),
    };

    private static string ContactText(ContactResult result) => result switch
    {
        ContactResult.NoContesto => "no contestó",
        ContactResult.LlamarDespues => "llamar después",
        ContactResult.NoPorAhora => "no por ahora",
        ContactResult.Agendo => "agendó",
        ContactResult.NoContactar => "no contactar",
        _ => result.ToString(),
    };

    private sealed record Visit(
        int Id, DateOnly Date, int RutaId, ParadaKind Kind, ParadaState State, int? PedidoId, int? InvitadoId, string? Name,
        DateTimeOffset? ArrivedAt, bool HasPhoto, bool HasSignature)
    {
        public KpiRow Row(string text, decimal numerator, decimal denominator) =>
            new(Date, RutaId, PedidoId, InvitadoId, Name, text, numerator, denominator, InvitadoId);
    }

    private sealed record TicketData(
        DateOnly Date, int? RutaId, int PedidoId, int InvitadoId, string Name, InvitadoType Type, string Folio, int Garments, decimal? Amount)
    {
        public KpiRow Row(string text, decimal numerator, decimal denominator) =>
            new(Date, RutaId, PedidoId, InvitadoId, Name, text, numerator, denominator, InvitadoId);
    }

    private sealed record ServicePedido(DateOnly Date, int? RutaId, int PedidoId, int InvitadoId, string Name);
}
