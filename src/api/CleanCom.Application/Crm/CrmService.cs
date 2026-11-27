using CleanCom.Application.Common;
using CleanCom.Application.Invitados;
using CleanCom.Application.Operations;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Common;
using CleanCom.Domain.Crm;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Crm;

/// <summary>
/// Clientes (CRM): status of each invitado, the "Llamar hoy" list, contacts and follow-up
/// (docs/03-reglas-de-negocio.md, CRM). The status is calculated on read with <see cref="CrmRules"/>; the nightly job
/// that stores it arrives with Hangfire and will use the same rule.
/// </summary>
public class CrmService(
    ICleanComDbContext db,
    ISettingService settings,
    IBusinessClock clock,
    InvitadoService invitados,
    PedidoService pedidos,
    IValidator<SaveContactRequest> contactValidator,
    IValidator<SaveSeguimientoRequest> seguimientoValidator,
    IInvitadoAccounts accounts,
    CrmInvitadoService profile)
{
    private static readonly CrmStatus[] CallStatuses = [CrmStatus.LeToca, CrmStatus.EnRiesgo];

    /// <param name="callToday">Only "Llamar hoy": le toca or en riesgo, their ruta comes tomorrow, no future contact date, not "do not contact".</param>
    /// <param name="page">1-based; a page past the end returns the last one.</param>
    /// <param name="pageSize">Rows per page, from 1 to <see cref="MaxPageSize"/>.</param>
    /// <param name="type">Only invitados of this type (cliente final, convenio, empresa, tienda interna).</param>
    /// <param name="account">Only invitados with their app account in this state.</param>
    public async Task<CrmListDto> ListAsync(bool callToday, CrmStatus? status, int? rutaId, string? q, CancellationToken ct, int page = 1, int pageSize = DefaultPageSize,
        InvitadoType? type = null, AppAccountState? account = null)
    {
        var today = clock.Today;
        var all = await EvaluateAsync(null, ct);
        var nonWorkingTomorrow = await db.NonWorkingDays.AnyAsync(d => d.Active && d.Date == today.AddDays(1), ct);
        // Le toca or en riesgo whose ruta comes tomorrow, or sent to reactivation from Clientes y fuentes.
        bool InCallToday(Row r) =>
            !r.Dto.DoNotContact && r.Dto.Type != InvitadoType.TiendaInterna
            && (r.Dto.NextContactOn is null || r.Dto.NextContactOn <= today)
            && ((CallStatuses.Contains(r.Dto.Status) && !nonWorkingTomorrow && r.VisitDays.Contains(today.AddDays(1).DayOfWeek))
                || (r.CallRequestedOn is { } requested && requested <= today));

        var counted = all.Where(r => r.Dto.Type != InvitadoType.TiendaInterna).ToList();
        var counts = Enum.GetValues<CrmStatus>().Select(s => new CrmStatusCountDto(s, counted.Count(r => r.Dto.Status == s))).ToList();

        var text = q?.Trim() ?? "";
        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
        var rows = all
            .Where(r => !callToday || InCallToday(r))
            .Where(r => status is null || r.Dto.Status == status)
            .Where(r => rutaId is null || r.RutaId == rutaId)
            .Where(r => type is null || r.Dto.Type == type)
            .Where(r => account is null || r.Dto.Account == account)
            .Where(r => text.Length < 2
                || r.Dto.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || (r.Dto.Neighborhood?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (digits.Length >= 4 && r.Dto.WhatsAppPhone.Contains(digits, StringComparison.Ordinal)))
            .OrderBy(r => Array.IndexOf(Priority, r.Dto.Status))
            .ThenBy(r => r.Dto.SuggestedVisit ?? DateOnly.MaxValue)
            .ThenBy(r => r.Dto.Name)
            .Select(r => r.Dto)
            .ToList();

        var (pageRows, currentPage, size) = Paginate(rows, page, pageSize);
        return new CrmListDto(today, counted.Count, all.Count(InCallToday), counts, pageRows, rows.Count, currentPage, size);
    }

    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    /// <summary>The requested page, clamped: size between 1 and <see cref="MaxPageSize"/>, page between 1 and the last one.</summary>
    public static (IReadOnlyList<T> Rows, int Page, int PageSize) Paginate<T>(IReadOnlyList<T> rows, int page, int pageSize)
    {
        var size = Math.Clamp(pageSize, 1, MaxPageSize);
        var lastPage = Math.Max(1, (rows.Count + size - 1) / size);
        var current = Math.Clamp(page, 1, lastPage);
        return (rows.Skip((current - 1) * size).Take(size).ToList(), current, size);
    }

    public async Task<CrmFichaDto> FichaAsync(int id, CancellationToken ct)
    {
        var invitado = await db.Invitados.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && i.Active, ct)
            ?? throw new NotFoundException($"El invitado {id} no existe.");
        var row = (await EvaluateAsync(id, ct)).Single();
        var contacts = await db.Contacts.AsNoTracking()
            .Where(c => c.InvitadoId == id && c.Active)
            .OrderByDescending(c => c.ContactedAt)
            .Select(c => new ContactDto(c.Id, c.Channel, c.Result, c.Note, c.NextContactOn, c.ContactedAt))
            .ToListAsync(ct);
        var history = (await pedidos.ListAsync(DateOnly.MinValue, null, id, ct)).OrderByDescending(p => p.RecoleccionDate).ToList();
        return new CrmFichaDto(await invitados.GetAsync(id, ct), row.Dto, invitado.ManualStatus, invitado.FixedFrequencyDays, history, contacts,
            await profile.ValueAsync(id, ct), await profile.AccountAsync(invitado, ct));
    }

    /// <summary>
    /// Records a call or WhatsApp. Without a date, the next contact follows the result (settings); "no contactar" marks
    /// the invitado so they leave "Llamar hoy". Either way they leave the list until the next contact date.
    /// </summary>
    public async Task<CrmFichaDto> AddContactAsync(int id, SaveContactRequest r, CancellationToken ct)
    {
        await contactValidator.ValidateAndThrowAsync(r, ct);
        var invitado = await db.Invitados.FirstOrDefaultAsync(i => i.Id == id && i.Active, ct)
            ?? throw new NotFoundException($"El invitado {id} no existe.");
        var today = clock.Today;
        if (r.NextContactOn is { } chosen && chosen <= today)
        {
            throw new DomainException("La fecha para volver a contactar debe ser posterior a hoy.");
        }

        var days = new FollowUpDays(
            await settings.GetIntegerAsync(SettingKeys.CrmNoAnswerDays, ct),
            await settings.GetIntegerAsync(SettingKeys.CrmCallLaterDays, ct),
            await settings.GetIntegerAsync(SettingKeys.CrmNotNowDays, ct));
        var next = r.NextContactOn ?? Contact.DefaultNextContact(r.Result, today, days);

        db.Contacts.Add(new Contact
        {
            InvitadoId = id,
            Channel = r.Channel,
            Result = r.Result,
            Note = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim(),
            NextContactOn = next,
            ContactedAt = clock.UtcNow,
        });
        invitado.NextContactOn = next;
        // The reactivation call was made.
        invitado.CallRequestedOn = null;
        if (r.Result == ContactResult.NoContactar)
        {
            invitado.DoNotContact = true;
        }
        await db.SaveChangesAsync(ct);
        return await FichaAsync(id, ct);
    }

    public async Task<CrmFichaDto> UpdateSeguimientoAsync(int id, SaveSeguimientoRequest r, CancellationToken ct)
    {
        await seguimientoValidator.ValidateAndThrowAsync(r, ct);
        var invitado = await db.Invitados.FirstOrDefaultAsync(i => i.Id == id && i.Active, ct)
            ?? throw new NotFoundException($"El invitado {id} no existe.");
        invitado.ManualStatus = r.ManualStatus;
        invitado.FixedFrequencyDays = r.FixedFrequencyDays;
        invitado.NextContactOn = r.NextContactOn;
        invitado.DoNotContact = r.DoNotContact;
        await db.SaveChangesAsync(ct);
        return await FichaAsync(id, ct);
    }

    // ---- Evaluation ----

    /// <summary>Order of the list: who needs attention first.</summary>
    private static readonly CrmStatus[] Priority =
        [CrmStatus.LeToca, CrmStatus.EnRiesgo, CrmStatus.Nuevo, CrmStatus.Activo, CrmStatus.Prospecto, CrmStatus.Inactivo];

    private sealed record Row(CrmRowDto Dto, int? RutaId, IReadOnlyCollection<DayOfWeek> VisitDays, DateOnly? CallRequestedOn);

    /// <summary>Evaluates every active invitado, or one.</summary>
    private async Task<IReadOnlyList<Row>> EvaluateAsync(int? onlyId, CancellationToken ct)
    {
        var today = clock.Today;
        var thresholds = new CrmThresholds(
            await settings.GetIntegerAsync(SettingKeys.CrmNewDays, ct),
            await settings.GetIntegerAsync(SettingKeys.CrmInactiveDays, ct),
            await settings.GetDecimalAsync(SettingKeys.CrmAtRiskFactor, ct));

        var people = await db.Invitados.AsNoTracking()
            .Where(i => i.Active && (onlyId == null || i.Id == onlyId))
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.WhatsAppPhone,
                i.Type,
                i.NextContactOn,
                i.DoNotContact,
                i.CallRequestedOn,
                i.UserId,
                i.InvitedToAppAt,
                i.ManualStatus,
                i.FixedFrequencyDays,
                // First active address with a zona: the one the CRM uses for the visit days.
                Address = i.Addresses.Where(d => d.Active).OrderBy(d => d.ZonaId == null).ThenBy(d => d.Id)
                    .Select(d => new { d.Neighborhood, ZonaCode = d.Zona == null ? null : d.Zona.Code, d.Zona })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var appUsers = await accounts.InfoAsync(people.Where(p => p.UserId != null).Select(p => p.UserId!.Value).ToList(), ct);
        var zonas = await db.Zonas.AsNoTracking().Include(z => z.Ruta).Include(z => z.VisitDays)
            .Include(z => z.ZonaNucleo!).ThenInclude(n => n.VisitDays)
            .ToDictionaryAsync(z => z.Id, ct);

        var services = await db.Pedidos.AsNoTracking()
            .Where(p => p.Active && p.Etapa != PedidoEtapa.Cancelado && (onlyId == null || p.InvitadoId == onlyId))
            .Select(p => new { p.InvitadoId, p.RecoleccionDate })
            .ToListAsync(ct);
        var byInvitado = services.ToLookup(s => s.InvitadoId, s => s.RecoleccionDate);

        return people.Select(p =>
        {
            var zona = p.Address?.Zona is { } z && zonas.TryGetValue(z.Id, out var full) ? full : null;
            IReadOnlyCollection<DayOfWeek> visitDays = zona?.EffectiveVisitDays() ?? [];
            var dates = byInvitado[p.Id].ToList();
            var nextVisit = CrmRules.NextVisitDay(today.AddDays(1), visitDays);
            var e = CrmRules.Evaluate(dates, today, nextVisit, thresholds, p.FixedFrequencyDays);
            var status = p.ManualStatus ?? e.Status;
            var why = p.ManualStatus is null ? e.Why : $"Estatus fijado por oficina (calculado: {Name(e.Status)}). {e.Why}";
            var suggested = e.LeTocaDate is { } leToca
                ? CrmRules.NextVisitDay(leToca > today ? leToca : today.AddDays(1), visitDays)
                : null;
            var nextRecoleccion = dates.Where(d => d > today).Order().Cast<DateOnly?>().FirstOrDefault();

            return new Row(
                new CrmRowDto(p.Id, p.Name, p.WhatsAppPhone, p.Type, p.Address?.Neighborhood, p.Address?.ZonaCode, zona?.Ruta?.Code,
                    status, p.ManualStatus is not null, why, e.ServiceCount, e.FrequencyDays, e.LastService, e.LeTocaDate,
                    suggested, nextRecoleccion, p.NextContactOn, p.DoNotContact, p.CallRequestedOn is { } requested && requested <= today,
                    InvitadoAccount.State(p.UserId is { } u && appUsers.ContainsKey(u), p.UserId is { } v && appUsers.TryGetValue(v, out var info) && info.Active, p.InvitedToAppAt)),
                zona?.RutaId,
                visitDays,
                p.CallRequestedOn);
        }).ToList();
    }

    private static string Name(CrmStatus s) => s switch
    {
        CrmStatus.LeToca => "le toca",
        CrmStatus.EnRiesgo => "en riesgo",
        _ => s.ToString().ToLowerInvariant(),
    };
}
