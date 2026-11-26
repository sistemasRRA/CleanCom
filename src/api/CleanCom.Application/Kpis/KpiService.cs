using CleanCom.Application.Access;
using CleanCom.Application.Common;
using CleanCom.Domain.Kpis;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Kpis;

/// <summary>
/// KPIs (docs/pantallas/oficina/kpis.md): the value of each indicator per period and ruta, saved by the nightly job
/// (<see cref="RecalculateOpenAsync"/>), with its goal in force and its traffic light; the detail that forms it and the
/// last weeks.
/// </summary>
public class KpiService(ICleanComDbContext db, IBusinessClock clock, KpiCalculator calculator, IUserService users)
{
    public const int DefaultHistoryWeeks = 8;
    private const int MaxHistoryWeeks = 26;

    public const string ExternalSources =
        "HubSpot, pauta y nómina no se calculan aquí; en la fase 2 se cruzan en BigQuery. Ticket promedio y facturación vienen del POS.";

    public async Task<KpiBoardDto> BoardAsync(KpiPeriod period, DateOnly? date, CancellationToken ct)
    {
        var today = clock.Today;
        var range = KpiRules.Range(period, date ?? today);
        var previous = KpiRules.Previous(period, range.Start);
        var current = await ValuesAsync(period, range, false, ct);
        var before = await ValuesAsync(period, previous, false, ct);
        var rutas = await RutasAsync(ct);
        var goals = await db.KpiGoals.AsNoTracking().Where(g => g.Active).ToListAsync(ct);
        var names = await users.NamesAsync(goals.Where(g => g.ResponsableUserId is not null).Select(g => g.ResponsableUserId!.Value).Distinct().ToList(), ct);
        var rutaIds = rutas.Select(r => r.Id).ToList();

        var cards = KpiCatalog.All.Select(kpi =>
        {
            var value = current.Total(kpi.Key);
            var goal = KpiGoalResolver.Resolve(kpi, goals, rutaIds, period, range, today);
            var byRuta = rutas.Select(r =>
            {
                var v = current.ForRuta(kpi.Key, r.Id);
                var t = goal.ByRuta[r.Id];
                return new KpiRutaValueDto(r.Id, r.Code, v, KpiRules.Light(v, t, kpi.HigherIsBetter), t?.Green);
            }).ToList();
            var g = goal.Goal;
            var applied = goal.Total is not { } total ? null : new KpiAppliedGoalDto(
                g?.Period, total.Red, total.Green, total.SuperGreen,
                g is not null && total != new KpiThresholds(g.Red, g.Green, g.SuperGreen), goal.FromRutas,
                g?.ResponsableUserId, g?.ResponsableUserId is { } id ? names.GetValueOrDefault(id) : null, g?.ValidFrom);
            var prev = before.Total(kpi.Key);
            return new KpiCardDto(kpi.Key, kpi.Name, kpi.Area, kpi.Unit, kpi.HigherIsBetter, kpi.Calculation, kpi.Source,
                value, prev, Trend(value, prev), byRuta, applied, goal.Note, KpiRules.Light(value, goal.Total, kpi.HigherIsBetter));
        }).ToList();

        return new KpiBoardDto(
            period, range.Start, range.End, KpiRules.Label(period, range), range.Contains(today), KpiRules.Label(period, previous),
            current.CalculatedAt,
            new KpiLightCountsDto(
                cards.Count(c => c.Light == KpiLight.SuperVerde),
                cards.Count(c => c.Light == KpiLight.Verde),
                cards.Count(c => c.Light == KpiLight.Rojo),
                cards.Count(c => c.Light == KpiLight.SinMeta)),
            rutas.Select(r => new KpiRutaDto(r.Id, r.Code, r.Color)).ToList(),
            cards,
            ExternalSources);
    }

    /// <summary>Calculates the period again now (data corrected after the night, or today's operations).</summary>
    public async Task<KpiBoardDto> RecalculateAsync(KpiPeriod period, DateOnly? date, CancellationToken ct)
    {
        await ValuesAsync(period, KpiRules.Range(period, date ?? clock.Today), true, ct);
        return await BoardAsync(period, date, ct);
    }

    /// <summary>
    /// The nightly job: the open week, month and quarter and the ones before (yesterday may belong to a period that just
    /// closed). Closed periods keep their saved values unless someone recalculates them.
    /// </summary>
    public async Task<int> RecalculateOpenAsync(CancellationToken ct)
    {
        var today = clock.Today;
        var count = 0;
        foreach (var period in Enum.GetValues<KpiPeriod>())
        {
            await ValuesAsync(period, KpiRules.Range(period, today), true, ct);
            await ValuesAsync(period, KpiRules.Previous(period, today), true, ct);
            count += 2;
        }
        return count;
    }

    public async Task<KpiDetailDto> DetailAsync(string key, KpiPeriod period, DateOnly? date, int? rutaId, CancellationToken ct)
    {
        var kpi = KpiCatalog.Find(key) ?? throw new NotFoundException($"El indicador {key} no existe.");
        var range = KpiRules.Range(period, date ?? clock.Today);
        var rows = (await calculator.RowsAsync(key, range, ct)).Where(r => rutaId is null || r.RutaId == rutaId).ToList();
        var codes = await db.Rutas.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.Code, ct);
        return new KpiDetailDto(kpi.Key, kpi.Name, kpi.Unit, kpi.Calculation, KpiRules.Label(period, range), rutaId,
            KpiRules.Value(kpi, rows.Select(r => r.Fact)),
            rows.Select(r => new KpiDetailRowDto(r.Date, r.RutaId is { } id ? codes.GetValueOrDefault(id) : null, r.PedidoId, r.InvitadoId,
                r.Name, r.Text, r.Numerator, r.Denominator)).ToList());
    }

    /// <summary>The last <paramref name="weeks"/> weeks up to the current one, oldest first.</summary>
    public async Task<KpiHistoryDto> HistoryAsync(int weeks, CancellationToken ct)
    {
        weeks = Math.Clamp(weeks, 1, MaxHistoryWeeks);
        var today = clock.Today;
        var ranges = Enumerable.Range(0, weeks).Reverse()
            .Select(i => KpiRules.Range(KpiPeriod.Semana, today.AddDays(-7 * i)))
            .ToList();
        var goals = await db.KpiGoals.AsNoTracking().Where(g => g.Active).ToListAsync(ct);
        var rutaIds = await db.Rutas.AsNoTracking().Where(r => r.Active).Select(r => r.Id).ToListAsync(ct);
        var values = new List<PeriodValues>();
        foreach (var range in ranges)
        {
            values.Add(await ValuesAsync(KpiPeriod.Semana, range, false, ct));
        }

        var rows = KpiCatalog.All.Select(kpi => new KpiHistoryRowDto(kpi.Key, kpi.Name, kpi.Area, kpi.Unit,
            ranges.Select((range, i) =>
            {
                var value = values[i].Total(kpi.Key);
                var goal = KpiGoalResolver.Resolve(kpi, goals, rutaIds, KpiPeriod.Semana, range, today);
                return new KpiHistoryCellDto(value, KpiRules.Light(value, goal.Total, kpi.HigherIsBetter));
            }).ToList())).ToList();
        return new KpiHistoryDto(
            ranges.Select(r => new KpiHistoryWeekDto(r.Start, r.End, $"{r.Start:dd/MM}")).ToList(),
            rows);
    }

    /// <summary>
    /// The saved values of a period; when there are none yet (or <paramref name="recalculate"/>), it calculates every
    /// indicator, in total and per ruta, and saves them.
    /// </summary>
    private async Task<PeriodValues> ValuesAsync(KpiPeriod period, DateRange range, bool recalculate, CancellationToken ct)
    {
        if (range.Start > clock.Today)
        {
            // Nothing has happened yet; saving empty values would hide the real ones later.
            return new PeriodValues([]);
        }
        var saved = await db.KpiValues
            .Where(v => v.Period == period && v.PeriodStart == range.Start)
            .ToListAsync(ct);
        var rutaIds = await db.Rutas.AsNoTracking().Where(r => r.Active).Select(r => r.Id).ToListAsync(ct);
        // Complete: every indicator in total and for every active ruta (a ruta created after the calculation is missing).
        var complete = KpiCatalog.All.All(k => saved.Any(v => v.IndicadorKey == k.Key && v.RutaId == null)
            && rutaIds.All(id => saved.Any(v => v.IndicadorKey == k.Key && v.RutaId == id)));
        if (complete && !recalculate)
        {
            return new PeriodValues(saved);
        }

        var now = clock.UtcNow;
        foreach (var kpi in KpiCatalog.All)
        {
            var facts = (await calculator.RowsAsync(kpi.Key, range, ct)).Select(r => r.Fact).ToList();
            Save(kpi.Key, null, KpiRules.Value(kpi, facts));
            foreach (var rutaId in rutaIds)
            {
                Save(kpi.Key, rutaId, KpiRules.Value(kpi, facts.Where(f => f.RutaId == rutaId)));
            }
        }
        await db.SaveChangesAsync(ct);
        return new PeriodValues(saved);

        void Save(string key, int? rutaId, decimal? value)
        {
            var row = saved.FirstOrDefault(v => v.IndicadorKey == key && v.RutaId == rutaId);
            if (row is null)
            {
                row = new KpiValue { IndicadorKey = key, Period = period, PeriodStart = range.Start, RutaId = rutaId };
                db.KpiValues.Add(row);
                saved.Add(row);
            }
            row.Value = value;
            row.CalculatedAt = now;
        }
    }

    private async Task<List<RutaRef>> RutasAsync(CancellationToken ct) =>
        await db.Rutas.AsNoTracking().Where(r => r.Active).OrderBy(r => r.Code)
            .Select(r => new RutaRef(r.Id, r.Code, r.Color)).ToListAsync(ct);

    private static KpiTrend Trend(decimal? value, decimal? previous) =>
        value is not { } v || previous is not { } p ? KpiTrend.SinDatos
        : v > p ? KpiTrend.Sube
        : v < p ? KpiTrend.Baja
        : KpiTrend.Igual;

    private sealed record RutaRef(int Id, string Code, string? Color);

    private sealed class PeriodValues(IReadOnlyList<KpiValue> values)
    {
        public DateTimeOffset? CalculatedAt { get; } = values.Count == 0 ? null : values.Min(v => v.CalculatedAt);

        public decimal? Total(string key) => ForRuta(key, null);

        public decimal? ForRuta(string key, int? rutaId) =>
            values.Where(v => v.IndicadorKey == key && v.RutaId == rutaId).OrderByDescending(v => v.Id).Select(v => v.Value).FirstOrDefault();
    }
}
