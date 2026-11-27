using System.Globalization;
using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Common;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Evidencias;

/// <summary>
/// Evidencias y caja, visits (docs/pantallas/oficina/evidencias-y-caja.md): every closed visit with its photos,
/// signature, ticket and reference payment, its totals, the Excel export and the gallery of a pedido.
/// </summary>
public class EvidenciasService(ICleanComDbContext db, IBusinessClock clock, ISettingService settings)
{
    public const int MaxDays = 92;
    public const int MaxExportRows = 20_000;

    /// <summary>One page of the visits in the filters, with the totals of all pages.</summary>
    public async Task<VisitasDto> ListAsync(VisitasQuery q, CancellationToken ct)
    {
        var (from, to) = Range(q);
        var page = Math.Max(1, q.Pagina);
        var pageSize = Math.Clamp(q.PorPagina, 1, 200);
        var tolerance = await settings.GetIntegerAsync(SettingKeys.GpsToleranceMeters, ct);
        var visits = Filtered(q, from, to);

        var counts = await visits.GroupBy(p => p.State).Select(g => new { State = g.Key, Count = g.Count() }).ToListAsync(ct);
        var payments = await db.ReferencePayments.AsNoTracking()
            .Where(r => r.Active && (q.FormaPago == null || r.Method == q.FormaPago) && visits.Any(p => p.Id == r.ParadaId))
            .GroupBy(r => r.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(r => r.Amount), Count = g.Count() })
            .ToListAsync(ct);
        var outside = await visits.CountAsync(p => p.ArrivedOutsideRadius
            || p.Evidence.Any(e => e.Active && e.Kind == EvidenceKind.Foto && e.DistanceMeters > tolerance), ct);
        var rowCount = counts.Sum(c => c.Count);

        var rows = await RowsAsync(visits.Skip((page - 1) * pageSize).Take(pageSize), tolerance, ct);
        var totals = new VisitasTotalsDto(
            counts.Where(c => c.State == ParadaState.Realizada).Sum(c => c.Count),
            counts.Where(c => c.State == ParadaState.NoRealizada).Sum(c => c.Count),
            payments.Sum(p => p.Amount),
            payments.OrderBy(p => p.Method).Select(p => new MethodTotalDto(p.Method, p.Amount, p.Count)).ToList(),
            outside);
        return new VisitasDto(from, to, rows, rowCount, page, pageSize, totals, tolerance);
    }

    /// <summary>The visits in the filters as an Excel workbook (every page, up to <see cref="MaxExportRows"/>).</summary>
    public async Task<(byte[] Content, string FileName)> ExportAsync(VisitasQuery q, CancellationToken ct)
    {
        var (from, to) = Range(q);
        var tolerance = await settings.GetIntegerAsync(SettingKeys.GpsToleranceMeters, ct);
        var visits = Filtered(q, from, to);
        if (await visits.CountAsync(ct) > MaxExportRows)
        {
            throw new DomainException($"Son más de {MaxExportRows:N0} visitas; acorta el periodo o filtra por ruta.");
        }
        var rows = await RowsAsync(visits, tolerance, ct);
        string[] header =
        [
            "Fecha", "Ruta", "Chofer", "Tipo", "Estado", "Invitado o parada", "Pedido", "Tickets", "Hora de cierre", "Prendas",
            "Forma de pago", "Monto", "Fotos", "Foto o llegada fuera de radio", "Firma", "Notas", "Motivo",
        ];
        var content = XlsxWriter.Write("Visitas", header, rows.Select(v => (IReadOnlyList<object?>)
        [
            v.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            v.RutaCode,
            v.Chofer,
            KindName(v.Kind),
            v.State == ParadaState.Realizada ? "Realizada" : "No realizada",
            v.Name,
            v.PedidoId,
            string.Join(", ", v.Tickets),
            v.ClosedAt is { } closed ? BusinessTime.TimeOfDay(closed).ToString("HH:mm", CultureInfo.InvariantCulture) : null,
            v.Garments,
            v.PaymentMethod?.ToString(),
            v.PaymentAmount,
            v.Photos.Count,
            v.ArrivedOutsideRadius || v.Photos.Any(p => p.OutsideRadius) ? "Sí" : "No",
            v.SignatureId is null ? "No" : "Sí",
            v.Notes,
            v.Reason,
        ]));
        var name = from == to ? $"visitas-{from:yyyy-MM-dd}.xlsx" : $"visitas-{from:yyyy-MM-dd}-a-{to:yyyy-MM-dd}.xlsx";
        return (content, name);
    }

    /// <summary>The photos and signature of every visit of a pedido (recolección and entrega), oldest first.</summary>
    public async Task<PedidoEvidenceDto> PedidoAsync(int pedidoId, CancellationToken ct)
    {
        var invitado = await db.Pedidos.AsNoTracking().Where(p => p.Id == pedidoId && p.Active)
            .Select(p => p.Invitado!.Name).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("El pedido no existe.");
        var tolerance = await settings.GetIntegerAsync(SettingKeys.GpsToleranceMeters, ct);
        var visits = await db.Paradas.AsNoTracking()
            .Where(p => p.Active && p.PedidoId == pedidoId)
            .OrderBy(p => p.RutaDia!.Date).ThenBy(p => p.ClosedAt)
            .Select(p => new
            {
                p.Id,
                p.RutaDia!.Date,
                p.Kind,
                p.State,
                p.RutaDia.Ruta!.Code,
                p.ClosedAt,
                Photos = p.Evidence.Where(e => e.Active && e.Kind == EvidenceKind.Foto).OrderBy(e => e.TakenAt)
                    .Select(e => new { e.Id, e.TakenAt, e.DistanceMeters }).ToList(),
                SignatureId = p.Evidence.Where(e => e.Active && e.Kind == EvidenceKind.Firma).Select(e => (int?)e.Id).FirstOrDefault(),
            })
            .ToListAsync(ct);
        return new PedidoEvidenceDto(pedidoId, invitado, visits.Select(v => new PedidoVisitaEvidenceDto(
            v.Id, v.Date, v.Kind, v.State, v.Code, v.ClosedAt,
            v.Photos.Select(e => new VisitaPhotoDto(e.Id, e.TakenAt, e.DistanceMeters, e.DistanceMeters > tolerance)).ToList(),
            v.SignatureId)).ToList());
    }

    private (DateOnly From, DateOnly To) Range(VisitasQuery q)
    {
        var from = q.Desde ?? q.Hasta ?? clock.Today;
        var to = q.Hasta ?? (q.Desde is null ? clock.Today : from);
        if (to < from)
        {
            throw new DomainException("La fecha final es antes de la inicial.");
        }
        if (to.DayNumber - from.DayNumber >= MaxDays)
        {
            throw new DomainException($"Consulta hasta {MaxDays} días a la vez.");
        }
        return (from, to);
    }

    /// <summary>Closed visits (done or not) of the period, newest first. Salida, regreso and comida are not visits.</summary>
    private IQueryable<Parada> Filtered(VisitasQuery q, DateOnly from, DateOnly to)
    {
        var visits = db.Paradas.AsNoTracking().Where(p => p.Active && p.ClosedAt != null
            && (p.State == ParadaState.Realizada || p.State == ParadaState.NoRealizada)
            && p.Kind != ParadaKind.Salida && p.Kind != ParadaKind.Regreso && p.Kind != ParadaKind.Comida
            && p.RutaDia!.Date >= from && p.RutaDia.Date <= to);
        if (q.RutaId is { } rutaId)
        {
            visits = visits.Where(p => p.RutaDia!.RutaId == rutaId);
        }
        if (q.FormaPago is { } method)
        {
            visits = visits.Where(p => db.ReferencePayments.Any(r => r.ParadaId == p.Id && r.Active && r.Method == method));
        }
        return visits.OrderByDescending(p => p.RutaDia!.Date).ThenByDescending(p => p.ClosedAt).ThenByDescending(p => p.Id);
    }

    private async Task<IReadOnlyList<VisitaDto>> RowsAsync(IQueryable<Parada> visits, int tolerance, CancellationToken ct)
    {
        var rows = await visits.Select(p => new
            {
                p.Id,
                p.RutaDia!.Date,
                p.RutaDia.RutaId,
                p.RutaDia.OperadorId,
                p.RutaDia.Ruta!.Code,
                p.RutaDia.Ruta.Color,
                p.Kind,
                p.State,
                Invitado = p.Pedido == null ? null : p.Pedido.Invitado!.Name,
                p.Title,
                Lugar = p.Lugar == null ? null : p.Lugar.Name,
                p.PedidoId,
                p.ClosedAt,
                p.GarmentsCollected,
                p.CloseNotes,
                p.NoRealizadaReasonId,
                p.ArrivedOutsideRadius,
                Photos = p.Evidence.Where(e => e.Active && e.Kind == EvidenceKind.Foto).OrderBy(e => e.TakenAt)
                    .Select(e => new { e.Id, e.TakenAt, e.DistanceMeters }).ToList(),
                SignatureId = p.Evidence.Where(e => e.Active && e.Kind == EvidenceKind.Firma).Select(e => (int?)e.Id).FirstOrDefault(),
                Payment = db.ReferencePayments.Where(r => r.ParadaId == p.Id && r.Active)
                    .Select(r => new { r.Method, r.Amount }).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var pedidoIds = rows.Where(r => r.PedidoId != null).Select(r => r.PedidoId!.Value).Distinct().ToList();
        var tickets = (await db.Tickets.AsNoTracking().Where(t => t.Active && pedidoIds.Contains(t.PedidoId))
                .Select(t => new { t.PedidoId, t.Folio }).ToListAsync(ct))
            .ToLookup(t => t.PedidoId, t => t.Folio);
        var reasonIds = rows.Where(r => r.NoRealizadaReasonId != null).Select(r => r.NoRealizadaReasonId!.Value).Distinct().ToList();
        var reasons = await db.Reasons.AsNoTracking().Where(r => reasonIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Text, ct);
        var choferes = await ChoferNames.ForAsync(db, rows.Select(r => (r.RutaId, r.OperadorId)), ct);

        return rows.Select(r =>
        {
            var reason = r.State == ParadaState.NoRealizada
                ? string.Join(": ", new[] { r.NoRealizadaReasonId is { } id ? reasons.GetValueOrDefault(id) : null, r.CloseNotes }.Where(s => !string.IsNullOrWhiteSpace(s)))
                : null;
            return new VisitaDto(
                r.Id, r.Date, r.Code, r.Color, choferes(r.RutaId, r.OperadorId), r.Kind, r.State,
                r.Invitado ?? r.Title ?? r.Lugar ?? KindName(r.Kind),
                r.PedidoId,
                r.PedidoId is { } pedidoId ? tickets[pedidoId].Order().ToList() : [],
                r.ClosedAt, r.GarmentsCollected, r.Payment?.Method, r.Payment?.Amount,
                r.Photos.Select(e => new VisitaPhotoDto(e.Id, e.TakenAt, e.DistanceMeters, e.DistanceMeters > tolerance)).ToList(),
                r.SignatureId,
                r.State == ParadaState.Realizada ? r.CloseNotes : null,
                string.IsNullOrEmpty(reason) ? null : reason,
                r.ArrivedOutsideRadius);
        }).ToList();
    }

    public static string KindName(ParadaKind kind) => kind switch
    {
        ParadaKind.Recoleccion => "Recolección",
        ParadaKind.Entrega => "Entrega",
        ParadaKind.RecoleccionYEntrega => "Entrega y recolección",
        ParadaKind.Convenio => "Convenio",
        ParadaKind.Insumos => "Insumos",
        ParadaKind.Traspaso => "Traspaso",
        ParadaKind.Traslado => "Traslado",
        _ => kind.ToString(),
    };
}

/// <summary>
/// The chofer of a ruta on a day, as Rutas shows it: the operador the office assigned, or the one whose usual ruta it is.
/// </summary>
internal static class ChoferNames
{
    public static async Task<Func<int, int?, string?>> ForAsync(ICleanComDbContext db, IEnumerable<(int RutaId, int? OperadorId)> rutaDias, CancellationToken ct)
    {
        var list = rutaDias.ToList();
        var assignedIds = list.Where(d => d.OperadorId != null).Select(d => d.OperadorId!.Value).Distinct().ToList();
        var rutaIds = list.Where(d => d.OperadorId == null).Select(d => d.RutaId).Distinct().ToList();
        var assigned = await db.Operadores.AsNoTracking().Where(o => assignedIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name, ct);
        var usual = (await db.Operadores.AsNoTracking()
                .Where(o => o.Active && o.UsualRutaId != null && rutaIds.Contains(o.UsualRutaId.Value))
                .OrderBy(o => o.Name).Select(o => new { RutaId = o.UsualRutaId!.Value, o.Name }).ToListAsync(ct))
            .GroupBy(o => o.RutaId).ToDictionary(g => g.Key, g => g.First().Name);
        return (rutaId, operadorId) => operadorId is { } id ? assigned.GetValueOrDefault(id) : usual.GetValueOrDefault(rutaId);
    }
}
