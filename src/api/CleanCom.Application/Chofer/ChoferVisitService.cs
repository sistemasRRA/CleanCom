using System.Globalization;
using System.Text;
using CleanCom.Application.Agenda;
using CleanCom.Application.Avisos;
using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Avisos;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Geography;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using CleanCom.Application.Pedidos;
using CleanCom.Application.Promociones;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Chofer;

/// <summary>
/// Closing a visit in the chofer app (docs/pantallas/apps/chofer.md, step 5): photos and signature, done (moves the
/// pedido to Recolectado or Entregado) or not done (reschedules it). Every action carries the app's local id, so a
/// resend after losing signal is not saved twice.
/// </summary>
public class ChoferVisitService(
    ICleanComDbContext db,
    IBusinessClock clock,
    ISettingService settings,
    IFileStore files,
    AgendaService agenda,
    ChoferService chofer,
    BeneficioService beneficios,
    AvisoService avisos)
{
    public const long MaxPhotoBytes = 8 * 1024 * 1024;
    private const int MaxStrokes = 300;
    private const int MaxPoints = 20_000;

    /// <summary>Motivos for a visit not done (catalog of failed entregas, also used for recolecciones).</summary>
    public async Task<IReadOnlyList<ChoferReasonDto>> ReasonsAsync(CancellationToken ct) =>
        await db.Reasons.AsNoTracking().Where(r => r.Active && r.Type == ReasonType.FailedEntrega).OrderBy(r => r.Text)
            .Select(r => new ChoferReasonDto(r.Id, r.Text)).ToListAsync(ct);

    /// <summary>A photo of the visit (JPG, PNG or WebP, compressed on the phone).</summary>
    public async Task<EvidenceDto> UploadPhotoAsync(int paradaId, Stream content, string? contentType, long length,
        string? localId, double? lat, double? lng, DateTimeOffset? takenAt, CancellationToken ct)
    {
        if (await ExistingAsync(localId, ct) is { } existing)
        {
            return existing;
        }
        var (parada, _) = await chofer.OwnParadaAsync(paradaId, ct);
        if (length <= 0 || length > MaxPhotoBytes)
        {
            throw new DomainException($"La foto debe pesar menos de {MaxPhotoBytes / 1024 / 1024} MB.");
        }
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var extension = FileSignatures.Image(buffer.GetBuffer().AsSpan(0, (int)buffer.Length))
            ?? throw new DomainException("La foto debe ser una imagen JPG, PNG o WebP.");
        buffer.Position = 0;
        var path = await files.SaveAsync(buffer, Folder(parada), extension, ct);
        var type = FileSignatures.ContentType(extension);
        return await AddAsync(parada, EvidenceKind.Foto, path, type, buffer.Length, localId, lat, lng, takenAt, ct);
    }

    /// <summary>The invitado's signature, drawn by the API as an SVG from the strokes.</summary>
    public async Task<EvidenceDto> SignAsync(SignatureRequest r, CancellationToken ct)
    {
        if (await ExistingAsync(r.LocalId, ct) is { } existing)
        {
            return existing;
        }
        var (parada, _) = await chofer.OwnParadaAsync(r.ParadaId, ct);
        if (parada.Kind is not (ParadaKind.Entrega or ParadaKind.RecoleccionYEntrega))
        {
            throw new DomainException("La firma es para las entregas.");
        }
        var svg = SignatureSvg(r);
        using var content = new MemoryStream(Encoding.UTF8.GetBytes(svg));
        var path = await files.SaveAsync(content, Folder(parada), ".svg", ct);
        return await AddAsync(parada, EvidenceKind.Firma, path, "image/svg+xml", content.Length, r.LocalId, null, null, r.TakenAt, ct);
    }

    /// <summary>The visit was done: recolección → Recolectado, entrega → Entregado.</summary>
    public async Task<ChoferRutaDto> CloseAsync(int paradaId, CloseVisitRequest r, CancellationToken ct)
    {
        var (parada, ordered) = await chofer.OwnParadaAsync(paradaId, ct);
        var date = parada.RutaDia!.Date;
        if (r.LocalId is not null && parada.CloseLocalId == r.LocalId)
        {
            return await chofer.RutaAsync(date, ct);
        }
        VisitClosing.EnsureCanClose(ordered.First(p => p.Id == paradaId), ordered, await OrderRequiredAsync(date, ct));
        var photos = await db.Evidence.CountAsync(e => e.ParadaId == paradaId && e.Active && e.Kind == EvidenceKind.Foto, ct);
        var photoRequired = await settings.GetBooleanAsync(SettingKeys.VisitPhotoRequired, ct);
        VisitClosing.EnsureComplete(parada.Kind, parada.PedidoId is not null,
            new VisitCloseData(r.Garments, r.PaymentMethod, r.PaymentAmount, photos), photoRequired);

        var at = chofer.ActionTime(r.At, date);
        Close(parada, ParadaState.Realizada, r.LocalId, r.Notes, r.Lat, r.Lng, at);
        parada.GarmentsCollected = r.Garments;
        if (r.PaymentMethod is { } method && r.PaymentAmount is { } amount)
        {
            db.ReferencePayments.Add(new ReferencePayment { ParadaId = parada.Id, Method = method, Amount = amount });
        }

        string? message = null;
        if (parada.PedidoId is { } pedidoId && VisitClosing.EtapaAfter(parada.Kind) is { } etapa
            && await db.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId && p.Active, ct) is { } pedido)
        {
            if (etapa == PedidoEtapa.Recolectado && r.Garments is { } garments)
            {
                pedido.GarmentsApprox = garments;
            }
            var note = etapa == PedidoEtapa.Recolectado
                ? $"Recolección cerrada por el chofer: {r.Garments} prenda{(r.Garments == 1 ? "" : "s")}."
                : "Entrega cerrada por el chofer.";
            if (PedidoFlow.Advance(pedido, etapa, at, note) is not null)
            {
                await avisos.EtapaAsync(pedido, etapa, ct);
                message = $"Pedido {pedido.Id}: {PedidoFlow.Name(etapa)}.";
                if (etapa == PedidoEtapa.Recolectado
                    && await beneficios.OnRecolectadoAsync(pedido, at, ct) is { Count: > 0 } granted)
                {
                    message += $" Se le dio: {string.Join(", ", granted)}.";
                }
            }
        }
        await db.SaveChangesAsync(ct);
        await chofer.NotifyParadaAsync(parada.Id, parada.RutaDiaId, date, ct);
        return await chofer.RutaAsync(date, ct) with { Message = message ?? "Visita cerrada." };
    }

    /// <summary>
    /// The visit was not done: motivo and photo. An entrega counts a failed attempt and a recolección is rescheduled,
    /// both to the next day with cupo from tomorrow. The notice to the office and the invitado arrives in week 6.
    /// </summary>
    public async Task<ChoferRutaDto> NotDoneAsync(int paradaId, NotDoneRequest r, CancellationToken ct)
    {
        var (parada, ordered) = await chofer.OwnParadaAsync(paradaId, ct);
        var date = parada.RutaDia!.Date;
        if (r.LocalId is not null && parada.CloseLocalId == r.LocalId)
        {
            return await chofer.RutaAsync(date, ct);
        }
        VisitClosing.EnsureCanClose(ordered.First(p => p.Id == paradaId), ordered, await OrderRequiredAsync(date, ct));
        var photos = await db.Evidence.CountAsync(e => e.ParadaId == paradaId && e.Active && e.Kind == EvidenceKind.Foto, ct);
        VisitClosing.EnsureNotDone(r.ReasonId, r.Note, photos, await settings.GetBooleanAsync(SettingKeys.VisitPhotoRequired, ct));
        if (r.ReasonId is { } reasonId && !await db.Reasons.AnyAsync(x => x.Id == reasonId && x.Type == ReasonType.FailedEntrega && x.Active, ct))
        {
            throw new DomainException("El motivo no existe o no es de visitas no realizadas.");
        }

        Close(parada, parada.State, r.LocalId, r.Note, r.Lat, r.Lng, chofer.ActionTime(r.At, date));
        parada.NoRealizadaReasonId = r.ReasonId;
        var etapa = parada.PedidoId is { } id
            ? await db.Pedidos.Where(p => p.Id == id && p.Active).Select(p => (PedidoEtapa?)p.Etapa).FirstOrDefaultAsync(ct)
            : null;

        string message;
        if (parada.Kind == ParadaKind.Entrega && etapa is PedidoEtapa.EntregaConfirmada or PedidoEtapa.EnCamino)
        {
            // Marks this parada as not done, counts the attempt and reschedules (Pedidos › No se entregó).
            var result = await agenda.NoEntregadoAsync(parada.PedidoId!.Value, new NoEntregadoRequest(r.ReasonId, r.Note), ct);
            message = result.Message;
        }
        else if (parada.Kind == ParadaKind.Recoleccion && etapa == PedidoEtapa.Agendado)
        {
            var result = await agenda.NoRecolectadoAsync(parada.PedidoId!.Value, r.ReasonId, r.Note, ct);
            message = $"Recolección no realizada. {result.Message}";
        }
        else
        {
            parada.State = ParadaState.NoRealizada;
            await db.SaveChangesAsync(ct);
            message = "Visita no realizada; oficina la verá.";
        }
        await chofer.NotifyParadaAsync(parada.Id, parada.RutaDiaId, date, ct);
        return await chofer.RutaAsync(date, ct) with { Message = message };
    }

    /// <summary>The order rule only applies to today's ruta; an action that syncs later is not blocked by it.</summary>
    private async Task<bool> OrderRequiredAsync(DateOnly date, CancellationToken ct) =>
        date == clock.Today && await settings.GetBooleanAsync(SettingKeys.VisitOrderRequired, ct);

    private static void Close(Parada parada, ParadaState state, string? localId, string? notes, double? lat, double? lng, DateTimeOffset at)
    {
        parada.State = state;
        parada.ClosedAt = at;
        parada.EnCaminoAt ??= at;
        parada.ArrivedAt ??= at;
        parada.ClosedLocation = lat is { } la && lng is { } ln ? Geo.CreatePoint(la, ln) : null;
        parada.CloseNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        parada.CloseLocalId = localId;
    }

    private async Task<EvidenceDto?> ExistingAsync(string? localId, CancellationToken ct) =>
        localId is null ? null : await db.Evidence.AsNoTracking().Where(e => e.LocalId == localId)
            .Select(e => new EvidenceDto(e.Id, e.ParadaId, e.Kind, e.TakenAt, e.DistanceMeters)).FirstOrDefaultAsync(ct);

    private async Task<EvidenceDto> AddAsync(Parada parada, EvidenceKind kind, string path, string contentType, long size,
        string? localId, double? lat, double? lng, DateTimeOffset? takenAt, CancellationToken ct)
    {
        var point = lat is { } la && lng is { } ln ? Geo.CreatePoint(la, ln) : null;
        var place = await chofer.ParadaLocationAsync(parada.Id, ct);
        var now = clock.UtcNow;
        var evidence = new Evidence
        {
            ParadaId = parada.Id,
            Kind = kind,
            FilePath = path,
            ContentType = contentType,
            SizeBytes = size,
            // Taken earlier without signal; never in the future.
            TakenAt = takenAt is { } t && t <= now ? t : now,
            Location = point,
            DistanceMeters = point is not null && place is not null
                ? ChoferFlow.DistanceMeters(new GeoPoint(point.Y, point.X), new GeoPoint(place.Y, place.X))
                : null,
            LocalId = string.IsNullOrWhiteSpace(localId) ? null : localId,
        };
        db.Evidence.Add(evidence);
        await db.SaveChangesAsync(ct);
        return new EvidenceDto(evidence.Id, evidence.ParadaId, kind, evidence.TakenAt, evidence.DistanceMeters);
    }

    private static string Folder(Parada parada) => $"evidencias/{parada.RutaDia!.Date:yyyy/MM}";

    /// <summary>Builds the SVG from numbers only, so nothing the phone sends becomes markup.</summary>
    public static string SignatureSvg(SignatureRequest r)
    {
        if (r.Width is <= 0 or > 4000 || r.Height is <= 0 or > 4000)
        {
            throw new DomainException("El tamaño de la firma no es válido.");
        }
        var strokes = r.Strokes.Where(s => s.Count >= 4).ToList();
        if (strokes.Count == 0)
        {
            throw new DomainException("Pide al invitado que firme antes de guardar.");
        }
        if (strokes.Count > MaxStrokes || strokes.Sum(s => s.Count) / 2 > MaxPoints)
        {
            throw new DomainException("La firma tiene demasiados trazos; bórrala y vuelve a firmar.");
        }
        var inv = CultureInfo.InvariantCulture;
        var w = r.Width.ToString("0.#", inv);
        var h = r.Height.ToString("0.#", inv);
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {w} {h}\" width=\"{w}\" height=\"{h}\">");
        sb.Append($"<rect width=\"{w}\" height=\"{h}\" fill=\"#fff\"/>");
        sb.Append("<path fill=\"none\" stroke=\"#111\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"");
        foreach (var s in strokes)
        {
            for (var i = 0; i + 1 < s.Count; i += 2)
            {
                var x = Math.Clamp(s[i], 0, r.Width);
                var y = Math.Clamp(s[i + 1], 0, r.Height);
                sb.Append(i == 0 ? 'M' : 'L').Append(x.ToString("0.#", inv)).Append(' ').Append(y.ToString("0.#", inv)).Append(' ');
            }
        }
        sb.Append("\"/></svg>");
        return sb.ToString();
    }
}
