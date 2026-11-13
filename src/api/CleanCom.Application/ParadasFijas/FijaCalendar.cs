using CleanCom.Application.Common;
using CleanCom.Domain.Routing;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.ParadasFijas;

/// <summary>
/// Which paradas fijas apply on which days. The agenda uses it so the fijas take cupo even beyond the days already
/// generated (docs/03-reglas-de-negocio.md, Paradas fijas, rule 2).
/// </summary>
public class FijaCalendar(ICleanComDbContext db)
{
    /// <summary>Active paradas fijas with their pasos and exceptions, optionally of one ruta.</summary>
    public async Task<List<ParadaFija>> LoadActiveAsync(int? rutaId, CancellationToken ct) =>
        await db.ParadasFijas.AsNoTracking()
            .Include(f => f.Pasos.Where(p => p.Active))
            .Include(f => f.Exceptions.Where(e => e.Active))
            .Where(f => f.Active && (rutaId == null || f.RutaId == rutaId))
            .ToListAsync(ct);

    public async Task<HashSet<DateOnly>> NonWorkingDaysAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        (await db.NonWorkingDays.AsNoTracking()
            .Where(d => d.Active && d.Date >= from && d.Date <= to)
            .Select(d => d.Date)
            .ToListAsync(ct)).ToHashSet();

    /// <summary>
    /// (parada fija, date) pairs that already have paradas, active or not. Once a day was generated, its paradas
    /// count in the agenda as any other parada; one the office took out of the ruta does not come back.
    /// </summary>
    public async Task<HashSet<(int FijaId, DateOnly Date)>> GeneratedAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        (await db.Paradas.AsNoTracking()
            .Where(p => p.ParadaFijaId != null && p.RutaDia!.Date >= from && p.RutaDia.Date <= to)
            .Select(p => new { FijaId = p.ParadaFijaId!.Value, p.RutaDia!.Date })
            .Distinct()
            .ToListAsync(ct))
        .Select(x => (x.FijaId, x.Date)).ToHashSet();

    /// <summary>
    /// Places the paradas fijas will take but have not been generated yet, per ruta and date (one per paso).
    /// The agenda adds them to the paradas it counts.
    /// </summary>
    public async Task<Dictionary<(int RutaId, DateOnly Date), int>> PendingCupoAsync(int? rutaId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var result = new Dictionary<(int, DateOnly), int>();
        var fijas = await LoadActiveAsync(rutaId, ct);
        if (fijas.Count == 0 || to < from)
        {
            return result;
        }

        var nonWorking = await NonWorkingDaysAsync(from, to, ct);
        var generated = await GeneratedAsync(from, to, ct);
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var fija in fijas)
            {
                if (fija.AppliesOn(date, nonWorking) && !generated.Contains((fija.Id, date)))
                {
                    var key = (fija.RutaId, date);
                    result[key] = result.GetValueOrDefault(key) + fija.ActivePasos.Count;
                }
            }
        }
        return result;
    }
}
