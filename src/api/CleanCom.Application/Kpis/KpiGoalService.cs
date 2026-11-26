using CleanCom.Application.Access;
using CleanCom.Application.Common;
using CleanCom.Domain.Access;
using CleanCom.Domain.Common;
using CleanCom.Domain.Kpis;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Kpis;

/// <summary>
/// Metas de los KPIs (WhatsApp y metas › Metas): rojo, verde y súper verde, period and responsable per indicator, for the
/// whole company or for one ruta, with vigencia. Changing a goal adds a new one from a date on; the past keeps the goal
/// it had.
/// </summary>
public class KpiGoalService(
    ICleanComDbContext db,
    IBusinessClock clock,
    IUserService users,
    KpiService kpis,
    IValidator<SaveKpiGoalRequest> validator)
{
    public async Task<KpiGoalsDto> ListAsync(CancellationToken ct)
    {
        var today = clock.Today;
        var goals = await db.KpiGoals.AsNoTracking().Where(g => g.Active).ToListAsync(ct);
        var rutas = await db.Rutas.AsNoTracking().OrderBy(r => r.Code).Select(r => new { r.Id, r.Code, r.Active }).ToListAsync(ct);
        var codes = rutas.ToDictionary(r => r.Id, r => r.Code);
        var ids = goals.SelectMany(g => new[] { g.ResponsableUserId, g.CreatedBy }).OfType<int>().Distinct().ToList();
        var names = await users.NamesAsync(ids, ct);
        var todayRange = new DateRange(today, today);
        // Last week as the KPIs screen shows it, so whoever sets a goal sees whether it is realistic.
        var lastWeek = await kpis.BoardAsync(KpiPeriod.Semana, today.AddDays(-7), ct);
        var cards = lastWeek.Indicadores.ToDictionary(c => c.Key);
        var weekRange = KpiRules.Range(KpiPeriod.Semana, today.AddDays(-7));
        var activeRutaIds = rutas.Where(r => r.Active).Select(r => r.Id).ToList();
        KpiGoalCheckDto Check(string key) => new(cards[key].Value, cards[key].Light, cards[key].Progress);
        KpiGoalCheckDto CheckRuta(string key, int rutaId)
        {
            if (cards[key].ByRuta.FirstOrDefault(r => r.RutaId == rutaId) is not { } r)
            {
                return new(null, KpiLight.SinMeta, null);
            }
            var kpi = KpiCatalog.Find(key)!;
            var thresholds = KpiGoalResolver.Resolve(kpi, goals, activeRutaIds, KpiPeriod.Semana, weekRange, today).ByRuta[rutaId];
            return new(r.Value, r.Light, KpiProgressRules.Of(r.Value, thresholds, kpi.HigherIsBetter));
        }

        KpiGoalDto? Dto(KpiGoal? g) => g is null ? null : new(g.Id, g.RutaId, g.RutaId is { } ruta ? codes.GetValueOrDefault(ruta) : null, g.Period,
            g.ResponsableUserId, g.ResponsableUserId is { } r ? names.GetValueOrDefault(r) : null, g.Red, g.Green, g.SuperGreen, g.ValidFrom,
            g.CreatedAt, g.CreatedBy is { } c ? names.GetValueOrDefault(c) : null);

        KpiGoal? Next(string key, int? rutaId) =>
            goals.Where(g => g.IndicadorKey == key && g.RutaId == rutaId && g.ValidFrom > today).OrderBy(g => g.ValidFrom).FirstOrDefault();

        var rows = KpiCatalog.All.Select(kpi => new KpiGoalRowDto(kpi.Key, kpi.Name, kpi.Area, kpi.Unit, kpi.HigherIsBetter, kpi.Scaling, kpi.Calculation,
            Dto(KpiRules.GoalFor(goals, kpi.Key, null, todayRange, today)),
            Dto(Next(kpi.Key, null)),
            rutas.Where(r => r.Active)
                .Select(r => new KpiRutaGoalDto(r.Id, r.Code, Dto(KpiRules.GoalFor(goals, kpi.Key, r.Id, todayRange, today)), Dto(Next(kpi.Key, r.Id)),
                    CheckRuta(kpi.Key, r.Id)))
                .ToList(),
            goals.Where(g => g.IndicadorKey == kpi.Key).OrderByDescending(g => g.ValidFrom).ThenBy(g => g.RutaId).ThenByDescending(g => g.Id)
                .Select(g => Dto(g)!).ToList(),
            Check(kpi.Key))).ToList();
        return new KpiGoalsDto(rows, await ResponsablesAsync(ct), lastWeek.Label);
    }

    /// <summary>
    /// Tries a goal being edited on last week, without saving it: the light and bar it would have given to the company
    /// (or the ruta) and what it equals per day, week, month and quarter. Mistakes come back as <see cref="KpiGoalPreviewDto.Error"/>.
    /// </summary>
    public async Task<KpiGoalPreviewDto> PreviewAsync(string key, SaveKpiGoalRequest r, CancellationToken ct)
    {
        var kpi = KpiCatalog.Find(key) ?? throw new NotFoundException($"El indicador {key} no existe.");
        var today = clock.Today;
        var week = KpiRules.Range(KpiPeriod.Semana, today.AddDays(-7));
        var board = await kpis.BoardAsync(KpiPeriod.Semana, week.Start, ct, r.RutaId);
        var value = board.Indicadores.Single(c => c.Key == key).Value;
        var goal = new KpiGoal { IndicadorKey = key, RutaId = r.RutaId, Period = r.Period, Red = r.Red, Green = r.Green, SuperGreen = r.SuperGreen, ValidFrom = week.Start };
        var error = KpiRules.OrderError(kpi.HigherIsBetter, r.Red, r.Green, r.SuperGreen)
            ?? (kpi.Scaling == KpiGoalScaling.SamePeriod && r.Period == KpiGoalPeriod.Dia ? $"{kpi.Name} cuenta invitados distintos: la meta es por semana o por mes." : null);
        var thresholds = error is null ? KpiRules.Scale(kpi, goal, KpiPeriod.Semana, week, week) : null;
        return new KpiGoalPreviewDto(
            board.Label, value, thresholds, KpiRules.Light(value, thresholds, kpi.HigherIsBetter),
            KpiProgressRules.Of(value, thresholds, kpi.HigherIsBetter),
            error is null ? KpiRules.Equivalences(kpi, goal).Select(e => new KpiEquivalenceDto(e.Label, e.Green)).ToList() : [],
            error,
            error is null ? KpiGoalResolver.Note(goal, thresholds) : null);
    }

    /// <summary>
    /// Sets the goal of an indicator, for the company or a ruta, from <see cref="SaveKpiGoalRequest.ValidFrom"/> (today
    /// when empty) on. A goal of the same ruta that already starts that same day is corrected in place; otherwise a new
    /// one is added.
    /// </summary>
    public async Task<KpiGoalsDto> SaveAsync(string key, SaveKpiGoalRequest r, CancellationToken ct)
    {
        var kpi = KpiCatalog.Find(key) ?? throw new NotFoundException($"El indicador {key} no existe.");
        await validator.ValidateAndThrowAsync(r, ct);
        var today = clock.Today;
        var from = r.ValidFrom ?? today;
        if (from < today)
        {
            throw new DomainException("La meta aplica de hoy en adelante; los periodos pasados conservan la meta que tenían.");
        }
        if (KpiRules.OrderError(kpi.HigherIsBetter, r.Red, r.Green, r.SuperGreen) is { } error)
        {
            throw new DomainException(error);
        }
        if (kpi.Scaling == KpiGoalScaling.SamePeriod && r.Period == KpiGoalPeriod.Dia)
        {
            throw new DomainException($"{kpi.Name} cuenta invitados distintos: la meta es por semana o por mes.");
        }
        if (r.RutaId is { } rutaId && !await db.Rutas.AnyAsync(x => x.Id == rutaId && x.Active, ct))
        {
            throw new DomainException("La ruta no existe o está inactiva.");
        }
        if (r.ResponsableUserId is { } responsable && (await ResponsablesAsync(ct)).All(u => u.Id != responsable))
        {
            throw new DomainException("El responsable debe ser un usuario activo de oficina.");
        }

        var goal = await db.KpiGoals.FirstOrDefaultAsync(g => g.Active && g.IndicadorKey == key && g.RutaId == r.RutaId && g.ValidFrom == from, ct);
        if (goal is null)
        {
            goal = new KpiGoal { IndicadorKey = key, RutaId = r.RutaId, ValidFrom = from };
            db.KpiGoals.Add(goal);
        }
        goal.Period = r.Period;
        goal.ResponsableUserId = r.ResponsableUserId;
        goal.Red = r.Red;
        goal.Green = r.Green;
        goal.SuperGreen = r.SuperGreen;
        await db.SaveChangesAsync(ct);
        return await ListAsync(ct);
    }

    /// <summary>Removes a goal that has not started yet (baja lógica). One already in force is replaced, not removed.</summary>
    public async Task<KpiGoalsDto> DeactivateAsync(string key, int id, CancellationToken ct)
    {
        var goal = await db.KpiGoals.FirstOrDefaultAsync(g => g.Id == id && g.IndicadorKey == key && g.Active, ct)
            ?? throw new NotFoundException("La meta no existe.");
        if (goal.ValidFrom <= clock.Today)
        {
            throw new DomainException("Esta meta ya está en vigor; para cambiarla, guarda una nueva desde hoy.");
        }
        goal.Active = false;
        await db.SaveChangesAsync(ct);
        return await ListAsync(ct);
    }

    /// <summary>Active office users: whoever answers for an indicator. Choferes and invitados are not.</summary>
    private async Task<List<KpiResponsableDto>> ResponsablesAsync(CancellationToken ct) =>
        (await users.ListAsync(null, ct))
            .Where(u => u.Active && u.Role != Roles.Chofer && u.Role != Roles.Invitado)
            .OrderBy(u => u.FullName)
            .Select(u => new KpiResponsableDto(u.Id, u.FullName, u.RoleName))
            .ToList();
}
