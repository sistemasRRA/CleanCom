using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Common;
using CleanCom.Domain.Fleet;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Fleet;

/// <summary>
/// Unidades (docs/pantallas/oficina/unidades.md): vehicles, their documents, logbook, recurring payments and the
/// notices of docs/03-reglas-de-negocio.md, Flota.
/// </summary>
public class UnidadService(
    ICleanComDbContext db,
    IBusinessClock clock,
    ISettingService settings,
    IFileStore files,
    IValidator<SaveUnidadRequest> unidadValidator,
    IValidator<SaveUnidadEventRequest> eventValidator,
    IValidator<SaveRecurringPaymentRequest> paymentValidator,
    IValidator<FleetAlertRuleDto> ruleValidator)
{
    public const long MaxFileBytes = 15 * 1024 * 1024;

    /// <summary>What the notices need besides the unidad: the days of each rule and the default service interval.</summary>
    private sealed record FleetContext(IReadOnlyDictionary<FleetAlertEvent, IReadOnlyList<int>> Days, int EveryKm, int EveryMonths, DateOnly Today);

    public async Task<UnidadesDto> ListAsync(bool includeInactive, CancellationToken ct)
    {
        var context = await ContextAsync(ct);
        var unidades = await Loaded().Where(u => includeInactive || u.Active).OrderBy(u => u.Plate).ToListAsync(ct);
        var cards = unidades.Select(u => Card(u, context)).ToList();
        return new UnidadesDto(cards, cards.Count(c => c.Active && c.Pending));
    }

    public async Task<UnidadDetailDto> GetAsync(int id, CancellationToken ct)
    {
        var unidad = await Loaded().FirstOrDefaultAsync(u => u.Id == id, ct) ?? throw new NotFoundException("La unidad no existe.");
        var context = await ContextAsync(ct);
        return new UnidadDetailDto(
            Card(unidad, context),
            unidad.GarmentCapacity,
            unidad.ServiceEveryKm,
            unidad.ServiceEveryMonths,
            unidad.Notes,
            unidad.Documents.Where(d => d.Active).OrderByDescending(d => d.CreatedAt)
                .Select(d => new UnidadDocumentDto(d.Id, d.Kind, d.ValidFrom, d.ValidUntil, d.Description, d.EventId, d.FilePath is not null, d.CreatedAt))
                .ToList(),
            unidad.Events.Where(e => e.Active).OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
                .Select(e => new UnidadEventDto(e.Id, e.Kind, e.Date, e.Km, e.Amount, e.Description, e.Severity, e.ResolvedOn, e.RecurringPaymentId))
                .ToList(),
            unidad.Payments.Where(p => p.Active).OrderBy(p => p.NextDate)
                .Select(p => new RecurringPaymentDto(p.Id, p.Concept, p.Amount, p.Frequency, p.NextDate, p.Active))
                .ToList());
    }

    public async Task<UnidadDetailDto> SaveAsync(int? id, SaveUnidadRequest r, CancellationToken ct)
    {
        await unidadValidator.ValidateAndThrowAsync(r, ct);
        var plate = NormalizePlate(r.Plate);
        if (r.Active && await db.Unidades.AnyAsync(u => u.Active && u.Plate == plate && u.Id != id, ct))
        {
            throw new ConflictException($"Ya existe la unidad con placa {plate}.");
        }
        var unidad = id is null ? new Unidad() : await db.Unidades.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("La unidad no existe.");
        unidad.Plate = plate;
        unidad.Brand = r.Brand.Trim();
        unidad.Model = r.Model.Trim();
        unidad.Year = r.Year;
        unidad.GarmentCapacity = r.GarmentCapacity;
        if (r.Km != unidad.Km)
        {
            unidad.Km = r.Km;
            unidad.KmUpdatedAt = clock.UtcNow;
        }
        unidad.ServiceEveryKm = r.ServiceEveryKm;
        unidad.ServiceEveryMonths = r.ServiceEveryMonths;
        unidad.State = r.State;
        unidad.Notes = Clean(r.Notes);
        unidad.Active = r.Active;
        if (id is null)
        {
            db.Unidades.Add(unidad);
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(unidad.Id, ct);
    }

    /// <summary>
    /// Registers a service, repair, falla, pago or odometer reading. A higher km updates the unidad's km; a pago of a
    /// recurring payment moves its next date forward.
    /// </summary>
    public async Task<UnidadDetailDto> AddEventAsync(int id, SaveUnidadEventRequest r, CancellationToken ct)
    {
        await eventValidator.ValidateAndThrowAsync(r, ct);
        var unidad = await db.Unidades.FirstOrDefaultAsync(u => u.Id == id && u.Active, ct)
            ?? throw new NotFoundException("La unidad no existe o está dada de baja.");
        if (r.Date > clock.Today)
        {
            throw new DomainException("La fecha no puede ser futura.");
        }
        if (r.Kind == UnidadEventKind.Kilometraje && r.Km < unidad.Km)
        {
            throw new DomainException($"El kilometraje no puede bajar: la unidad lleva {unidad.Km:N0} km.");
        }

        var amount = r.Amount;
        if (r.RecurringPaymentId is { } paymentId)
        {
            var payment = await db.RecurringPayments.FirstOrDefaultAsync(p => p.Id == paymentId && p.UnidadId == id && p.Active, ct)
                ?? throw new NotFoundException("El pago recurrente no existe.");
            amount ??= payment.Amount;
            payment.NextDate = FleetRules.NextPaymentDate(payment.NextDate, payment.Frequency);
        }

        db.UnidadEvents.Add(new UnidadEvent
        {
            UnidadId = id,
            Kind = r.Kind,
            Date = r.Date,
            Km = r.Km,
            Amount = amount,
            Description = Clean(r.Description) ?? DefaultDescription(r.Kind),
            Severity = r.Severity,
            RecurringPaymentId = r.RecurringPaymentId,
        });
        if (r.Km is { } km && km > unidad.Km)
        {
            unidad.Km = km;
            unidad.KmUpdatedAt = clock.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Closes a falla: it no longer counts as open.</summary>
    public async Task<UnidadDetailDto> ResolveFailureAsync(int id, int eventId, CancellationToken ct)
    {
        var falla = await db.UnidadEvents.FirstOrDefaultAsync(e => e.Id == eventId && e.UnidadId == id && e.Active, ct)
            ?? throw new NotFoundException("El registro no existe.");
        if (falla.Kind != UnidadEventKind.Falla)
        {
            throw new DomainException("Solo una falla se marca como resuelta.");
        }
        falla.ResolvedOn ??= clock.Today;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>
    /// A document: póliza, tarjeta de circulación, verificación (with the day it was done in validFrom) or a receipt.
    /// The file is optional (PDF, JPG, PNG or WebP, up to 15 MB) and goes to the server's disk.
    /// </summary>
    public async Task<UnidadDetailDto> AddDocumentAsync(int id, UnidadDocumentKind kind, DateOnly? validFrom, DateOnly? validUntil,
        string? description, int? eventId, Stream? content, long length, CancellationToken ct)
    {
        if (!await db.Unidades.AnyAsync(u => u.Id == id && u.Active, ct))
        {
            throw new NotFoundException("La unidad no existe o está dada de baja.");
        }
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException("El tipo de documento no es válido.");
        }
        if (kind is UnidadDocumentKind.Seguro or UnidadDocumentKind.TarjetaCirculacion && validUntil is null)
        {
            throw new DomainException("Indica hasta cuándo es vigente.");
        }
        if (kind == UnidadDocumentKind.Verificacion && validFrom is null)
        {
            throw new DomainException("Indica la fecha en que se hizo la verificación.");
        }
        if (validFrom is { } from && validUntil is { } until && until < from)
        {
            throw new DomainException("La vigencia termina antes de empezar.");
        }
        if (eventId is { } e && !await db.UnidadEvents.AnyAsync(x => x.Id == e && x.UnidadId == id && x.Active, ct))
        {
            throw new NotFoundException("El registro de la bitácora no existe.");
        }
        if (description?.Length > 300)
        {
            throw new DomainException("La descripción admite hasta 300 caracteres.");
        }

        string? path = null, type = null;
        if (content is not null)
        {
            if (length <= 0 || length > MaxFileBytes)
            {
                throw new DomainException($"El archivo debe pesar menos de {MaxFileBytes / 1024 / 1024} MB.");
            }
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            var extension = FileSignatures.ImageOrPdf(buffer.GetBuffer().AsSpan(0, (int)buffer.Length))
                ?? throw new DomainException("El archivo debe ser PDF, JPG, PNG o WebP.");
            buffer.Position = 0;
            path = await files.SaveAsync(buffer, $"unidades/{id}", extension, ct);
            type = FileSignatures.ContentType(extension);
        }

        db.UnidadDocuments.Add(new UnidadDocument
        {
            UnidadId = id,
            Kind = kind,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            Description = Clean(description),
            EventId = eventId,
            FilePath = path,
            ContentType = type,
        });
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<(Stream Content, string ContentType)> OpenDocumentAsync(int id, int documentId, CancellationToken ct)
    {
        var document = await db.UnidadDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.UnidadId == id && d.Active, ct)
            ?? throw new NotFoundException("El documento no existe.");
        if (document.FilePath is null)
        {
            throw new NotFoundException("El documento no tiene archivo.");
        }
        var stream = files.OpenRead(document.FilePath) ?? throw new NotFoundException("El archivo no está en el servidor.");
        return (stream, document.ContentType ?? "application/octet-stream");
    }

    /// <summary>Removing a document is a baja lógica.</summary>
    public async Task<UnidadDetailDto> DeactivateDocumentAsync(int id, int documentId, CancellationToken ct)
    {
        var document = await db.UnidadDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.UnidadId == id, ct)
            ?? throw new NotFoundException("El documento no existe.");
        document.Active = false;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<UnidadDetailDto> SavePaymentAsync(int id, int? paymentId, SaveRecurringPaymentRequest r, CancellationToken ct)
    {
        await paymentValidator.ValidateAndThrowAsync(r, ct);
        if (!await db.Unidades.AnyAsync(u => u.Id == id && u.Active, ct))
        {
            throw new NotFoundException("La unidad no existe o está dada de baja.");
        }
        var payment = paymentId is null ? new RecurringPayment { UnidadId = id }
            : await db.RecurringPayments.FirstOrDefaultAsync(p => p.Id == paymentId && p.UnidadId == id, ct)
                ?? throw new NotFoundException("El pago recurrente no existe.");
        payment.Concept = r.Concept.Trim();
        payment.Amount = r.Amount;
        payment.Frequency = r.Frequency;
        payment.NextDate = r.NextDate;
        payment.Active = r.Active;
        if (paymentId is null)
        {
            db.RecurringPayments.Add(payment);
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>
    /// What is wrong with a unidad before assigning it to a ruta: out of service, an open falla or an expired document,
    /// in Spanish. Empty when it is fine.
    /// </summary>
    public async Task<IReadOnlyList<string>> IssuesAsync(int id, CancellationToken ct)
    {
        var unidad = await Loaded().FirstOrDefaultAsync(u => u.Id == id && u.Active, ct)
            ?? throw new NotFoundException("La unidad no existe o está dada de baja.");
        var card = Card(unidad, await ContextAsync(ct));
        var issues = new List<string>();
        if (unidad.State == UnidadState.FueraDeServicio)
        {
            issues.Add($"La unidad {unidad.Plate} está fuera de servicio.");
        }
        issues.AddRange(card.Alerts
            .Where(a => a.Level == FleetAlertLevel.Falla
                || (a.Level == FleetAlertLevel.Vencido && a.Event is FleetAlertEvent.Seguro or FleetAlertEvent.TarjetaCirculacion or FleetAlertEvent.Verificacion))
            .Select(a => a.Message));
        return issues;
    }

    public async Task<IReadOnlyList<FleetAlertRuleDto>> RulesAsync(CancellationToken ct) =>
        await db.FleetAlertRules.AsNoTracking().OrderBy(r => r.Id)
            .Select(r => new FleetAlertRuleDto(r.Event, r.DaysBefore, r.Recipients, r.Channels))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FleetAlertRuleDto>> SaveRulesAsync(IReadOnlyList<FleetAlertRuleDto> rules, CancellationToken ct)
    {
        foreach (var rule in rules)
        {
            await ruleValidator.ValidateAndThrowAsync(rule, ct);
        }
        var stored = await db.FleetAlertRules.ToListAsync(ct);
        foreach (var rule in rules)
        {
            var row = stored.FirstOrDefault(s => s.Event == rule.Event);
            if (row is null)
            {
                row = new FleetAlertRule { Event = rule.Event };
                db.FleetAlertRules.Add(row);
                stored.Add(row);
            }
            row.DaysBefore = string.Join(',', FleetRules.ParseDays(rule.DaysBefore));
            row.Recipients = string.Join(',', Split(rule.Recipients));
            row.Channels = string.Join(',', Split(rule.Channels));
        }
        await db.SaveChangesAsync(ct);
        return await RulesAsync(ct);
    }

    private IQueryable<Unidad> Loaded() =>
        db.Unidades.AsNoTracking()
            .Include(u => u.Documents.Where(d => d.Active))
            .Include(u => u.Events.Where(e => e.Active))
            .Include(u => u.Payments.Where(p => p.Active));

    private async Task<FleetContext> ContextAsync(CancellationToken ct)
    {
        var rules = await db.FleetAlertRules.AsNoTracking().ToListAsync(ct);
        return new FleetContext(
            rules.ToDictionary(r => r.Event, r => FleetRules.ParseDays(r.DaysBefore)),
            await settings.GetIntegerAsync(SettingKeys.UnidadServiceEveryKm, ct),
            await settings.GetIntegerAsync(SettingKeys.UnidadServiceEveryMonths, ct),
            clock.Today);
    }

    private static UnidadCardDto Card(Unidad u, FleetContext c)
    {
        IReadOnlyList<int> Days(FleetAlertEvent e) => c.Days.GetValueOrDefault(e) ?? [];

        var insurance = u.Documents.Where(d => d.Kind == UnidadDocumentKind.Seguro).Max(d => d.ValidUntil);
        var card = u.Documents.Where(d => d.Kind == UnidadDocumentKind.TarjetaCirculacion).Max(d => d.ValidUntil);
        var verified = u.Documents.Where(d => d.Kind == UnidadDocumentKind.Verificacion).Max(d => d.ValidFrom);
        var lastService = u.Events.Where(e => e.Kind == UnidadEventKind.Servicio).OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).FirstOrDefault();
        var due = FleetRules.NextService(lastService?.Date, lastService?.Km, u.ServiceEveryKm ?? c.EveryKm, u.ServiceEveryMonths ?? c.EveryMonths);
        var openFailures = u.Events.Where(e => e.Kind == UnidadEventKind.Falla && e.ResolvedOn is null).OrderByDescending(e => e.Date).ToList();

        var alerts = new List<FleetAlert?>
        {
            FleetRules.Verification(u.Plate, verified, c.Today, Days(FleetAlertEvent.Verificacion)),
            FleetRules.Document(FleetAlertEvent.Seguro, "seguro", insurance, c.Today, Days(FleetAlertEvent.Seguro)),
            FleetRules.Document(FleetAlertEvent.TarjetaCirculacion, "tarjeta de circulación", card, c.Today, Days(FleetAlertEvent.TarjetaCirculacion)),
            FleetRules.Service(due, u.Km, c.Today, Days(FleetAlertEvent.Servicio)),
        };
        alerts.AddRange(u.Payments.Select(p => FleetRules.Payment(p.Concept, p.NextDate, c.Today, Days(FleetAlertEvent.PagoRecurrente))));
        alerts.AddRange(openFailures.Select(f => new FleetAlert(FleetAlertEvent.Falla, FleetAlertLevel.Falla,
            $"Falla abierta ({f.Severity?.ToString().ToLowerInvariant()}): {f.Description}", f.Date, false)));
        if (u.State == UnidadState.FueraDeServicio)
        {
            alerts.Insert(0, new FleetAlert(FleetAlertEvent.Falla, FleetAlertLevel.Falla, "Fuera de servicio.", null, false));
        }
        var list = alerts.OfType<FleetAlert>().Select(a => new FleetAlertDto(a.Event, a.Level, a.Message, a.DueDate)).ToList();

        var yearStart = new DateOnly(c.Today.Year, 1, 1);
        var thisYear = u.Events.Where(e => e.Date >= yearStart).ToList();
        var spent = thisYear.Sum(e => e.Amount ?? 0);
        var kmAtStart = thisYear.Where(e => e.Km is not null).Select(e => e.Km).Min();

        return new UnidadCardDto(
            u.Id, u.Plate, u.Brand, u.Model, u.Year, u.Km, u.KmUpdatedAt, u.State, u.Active, list,
            list.Any(a => a.Level != FleetAlertLevel.PorVencer),
            lastService?.Date, lastService?.Km, due?.Date, due?.Km,
            spent, FleetRules.CostPerKm(spent, kmAtStart, u.Km),
            insurance, card, verified, openFailures.Count);
    }

    /// <summary>"abc-123 4" → "ABC1234": the plate without spaces or dashes, in capitals.</summary>
    public static string NormalizePlate(string plate) =>
        new string(plate.Where(char.IsAsciiLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string DefaultDescription(UnidadEventKind kind) => kind switch
    {
        UnidadEventKind.Servicio => "Servicio",
        UnidadEventKind.Pago => "Pago",
        UnidadEventKind.Kilometraje => "Lectura de kilometraje",
        _ => kind.ToString(),
    };

    private static IEnumerable<string> Split(string text) =>
        text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
