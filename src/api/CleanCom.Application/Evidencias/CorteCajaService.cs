using CleanCom.Application.Access;
using CleanCom.Application.Common;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Common;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Evidencias;

/// <summary>
/// Corte de caja (docs/pantallas/oficina/evidencias-y-caja.md): per ruta and day, what the chofer recorded in the
/// visits, what they declared at the end of the shift and what the POS says. The office closes it with a folio and,
/// with differences, a note. Once closed it does not change.
/// </summary>
public class CorteCajaService(
    ICleanComDbContext db,
    IBusinessClock clock,
    ICurrentUser currentUser,
    IUserService users,
    IValidator<CloseCorteRequest> closeValidator)
{
    /// <summary>The cortes of every ruta that has something that day (today when empty).</summary>
    public async Task<CortesDto> DayAsync(DateOnly? date, CancellationToken ct)
    {
        var day = date ?? clock.Today;
        var rutaDias = await db.RutaDias.AsNoTracking().Include(d => d.Ruta).Include(d => d.Unidad)
            .Where(d => d.Date == day && d.Ruta!.Active)
            .OrderBy(d => d.Ruta!.Code)
            .ToListAsync(ct);
        var ids = rutaDias.Select(d => d.Id).ToList();
        var registered = await RegisteredAsync(ids, ct);
        var cortes = await db.CortesCaja.AsNoTracking().Include(c => c.Lines).Where(c => ids.Contains(c.RutaDiaId)).ToDictionaryAsync(c => c.RutaDiaId, ct);
        var choferes = await ChoferNames.ForAsync(db, rutaDias.Select(d => (d.RutaId, d.OperadorId)), ct);
        var closers = await users.NamesAsync(cortes.Values.Where(c => c.ClosedBy != null).Select(c => c.ClosedBy!.Value).Distinct().ToList(), ct);

        var list = rutaDias
            // A ruta with nothing that day (no shift, no payments, no corte) has nothing to review.
            .Where(d => d.ShiftStartedAt != null || cortes.ContainsKey(d.Id) || registered.Any(r => r.Key.RutaDiaId == d.Id))
            .Select(d => Build(d, cortes.GetValueOrDefault(d.Id), registered, choferes(d.RutaId, d.OperadorId), closers))
            .ToList();
        return new CortesDto(day, AgendaCalculator.Format(day), list, list.Count(c => c.State == CorteState.PorRevisar));
    }

    /// <summary>
    /// The office closes a corte the chofer declared: the POS amounts (captured by hand until the POS is connected),
    /// a note (required with differences), the folio and who closed it. What the visits recorded is kept as it was.
    /// </summary>
    public async Task<CortesDto> CloseAsync(int corteId, CloseCorteRequest r, CancellationToken ct)
    {
        await closeValidator.ValidateAndThrowAsync(r, ct);
        var corte = await db.CortesCaja.Include(c => c.Lines).Include(c => c.RutaDia).ThenInclude(d => d!.Ruta)
            .FirstOrDefaultAsync(c => c.Id == corteId, ct)
            ?? throw new NotFoundException("El corte de caja no existe.");
        var registered = await RegisteredAsync([corte.RutaDiaId], ct);
        var pos = (r.Pos ?? []).ToDictionary(p => p.Method, p => p.Amount);
        var lines = CorteCajaRules.Lines(
            Enum.GetValues<PaymentMethod>().ToDictionary(m => m, m => registered.GetValueOrDefault((corte.RutaDiaId, m)).Amount),
            corte.Lines.ToDictionary(l => l.Method, l => l.Declared),
            pos);
        CorteCajaRules.EnsureCanClose(corte.ClosedAt is not null, lines, r.Notes);

        foreach (var line in lines)
        {
            var row = corte.Lines.FirstOrDefault(l => l.Method == line.Method);
            if (row is null)
            {
                row = new CorteCajaLine { Method = line.Method, Declared = 0 };
                corte.Lines.Add(row);
            }
            row.Registered = line.Registered;
            row.Pos = line.Pos;
        }
        corte.Folio = CorteCajaRules.Folio(corte.Date, corte.RutaDia!.Ruta!.Code);
        corte.ClosedAt = clock.UtcNow;
        corte.ClosedBy = currentUser.Id;
        corte.OfficeNotes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim();
        await db.SaveChangesAsync(ct);
        return await DayAsync(corte.Date, ct);
    }

    /// <summary>Reference payments recorded in the visits, per ruta del día and forma de pago.</summary>
    private async Task<Dictionary<(int RutaDiaId, PaymentMethod Method), (decimal Amount, int Count)>> RegisteredAsync(
        IReadOnlyCollection<int> rutaDiaIds, CancellationToken ct) =>
        (await db.ReferencePayments.AsNoTracking()
            .Where(r => r.Active)
            .Join(db.Paradas.Where(p => p.Active && rutaDiaIds.Contains(p.RutaDiaId)), r => r.ParadaId, p => p.Id, (r, p) => new { p.RutaDiaId, r.Method, r.Amount })
            .GroupBy(x => new { x.RutaDiaId, x.Method })
            .Select(g => new { g.Key.RutaDiaId, g.Key.Method, Amount = g.Sum(x => x.Amount), Count = g.Count() })
            .ToListAsync(ct))
        .ToDictionary(x => (x.RutaDiaId, x.Method), x => (x.Amount, x.Count));

    private static CorteDto Build(
        RutaDia d,
        CorteCaja? corte,
        Dictionary<(int RutaDiaId, PaymentMethod Method), (decimal Amount, int Count)> registered,
        string? chofer,
        IReadOnlyDictionary<int, string> closers)
    {
        var closed = corte?.ClosedAt is not null;
        var lines = Enum.GetValues<PaymentMethod>().Select(m =>
        {
            var live = registered.GetValueOrDefault((d.Id, m));
            var row = corte?.Lines.FirstOrDefault(l => l.Method == m);
            // Once closed, what the visits recorded then; before, what they record now.
            var recorded = closed ? row?.Registered ?? 0 : live.Amount;
            if (corte is null)
            {
                return new CorteLineDto(m, recorded, live.Count, null, null, null, null);
            }
            var line = new CorteLine(m, recorded, row?.Declared ?? 0, row?.Pos);
            return new CorteLineDto(m, recorded, live.Count, line.Declared, line.Pos, line.VsRegistered, line.VsPos);
        }).ToList();

        var state = corte is null
            ? d.ShiftStartedAt is null ? CorteState.SinTurno : CorteState.TurnoAbierto
            : closed ? CorteState.Cerrado : CorteState.PorRevisar;
        return new CorteDto(
            d.Id, corte?.Id, d.Ruta!.Code, d.Ruta.Color, chofer, d.Unidad?.Plate,
            d.ShiftStartedAt, d.ShiftEndedAt, d.StartKm, d.EndKm, state, lines,
            lines.Sum(l => l.Registered),
            corte is null ? null : lines.Sum(l => l.Declared ?? 0),
            lines.Any(l => l.VsRegistered is { } a && a != 0 || l.VsPos is { } b && b != 0),
            corte?.ChoferNotes, corte?.Folio, corte?.ClosedAt,
            corte?.ClosedBy is { } by ? closers.GetValueOrDefault(by) : null,
            corte?.OfficeNotes);
    }
}

public class CloseCorteValidator : AbstractValidator<CloseCorteRequest>
{
    public CloseCorteValidator()
    {
        RuleForEach(x => x.Pos).ChildRules(line =>
        {
            line.RuleFor(l => l.Method).IsInEnum().WithName("Forma de pago");
            line.RuleFor(l => l.Amount).InclusiveBetween(0, 1_000_000).WithName("Monto del punto de venta");
        });
        RuleFor(x => x.Pos)
            .Must(lines => lines!.Select(l => l.Method).Distinct().Count() == lines!.Count)
            .When(x => x.Pos is not null)
            .WithMessage("Captura cada forma de pago una sola vez.");
        RuleFor(x => x.Notes).MaximumLength(1000).WithName("Notas");
    }
}
