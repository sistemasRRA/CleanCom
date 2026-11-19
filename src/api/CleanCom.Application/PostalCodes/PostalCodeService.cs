using CleanCom.Application.Common;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.PostalCodes;

/// <summary>
/// Official postal code catalog (Correos de México). Imported from its TXT export; used to fill colonia and alcaldía.
/// </summary>
public class PostalCodeService(ICleanComDbContext db, IBulkInserter bulk, IBusinessClock clock, ICurrentUser currentUser)
{
    private const int SearchLimit = 100;

    /// <summary>Colonias of a postal code, to choose one when capturing an address.</summary>
    public async Task<IReadOnlyList<PostalCodeDto>> LookupAsync(string code, CancellationToken ct)
    {
        var cp = code.Trim();
        return await db.PostalCodes.AsNoTracking()
            .Where(p => p.Active && p.Code == cp)
            .OrderBy(p => p.Settlement)
            .Select(p => new PostalCodeDto(p.Code, p.Settlement, p.SettlementType, p.Borough, p.City, p.State))
            .ToListAsync(ct);
    }

    /// <summary>By postal code prefix or colonia name.</summary>
    public async Task<IReadOnlyList<PostalCodeDto>> SearchAsync(string? search, CancellationToken ct)
    {
        var text = search?.Trim() ?? "";
        if (text.Length < 2)
        {
            return [];
        }
        return await db.PostalCodes.AsNoTracking()
            .Where(p => p.Active && (p.Code.StartsWith(text) || p.Settlement.Contains(text)))
            .OrderBy(p => p.Code).ThenBy(p => p.Settlement)
            .Take(SearchLimit)
            .Select(p => new PostalCodeDto(p.Code, p.Settlement, p.SettlementType, p.Borough, p.City, p.State))
            .ToListAsync(ct);
    }

    public async Task<PostalCodeSummaryDto> SummaryAsync(CancellationToken ct)
    {
        // Two plain aggregates: a distinct count inside each group becomes one subquery per state and times out with
        // the catalog of the whole country.
        var settlements = await db.PostalCodes.AsNoTracking()
            .Where(p => p.Active)
            .GroupBy(p => p.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var codesByState = await db.PostalCodes.AsNoTracking()
            .Where(p => p.Active)
            .Select(p => new { p.State, p.Code })
            .Distinct()
            .GroupBy(p => p.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.State, x => x.Count, ct);
        var states = settlements
            .Select(s => new PostalCodeStateDto(s.State, s.Count, codesByState.GetValueOrDefault(s.State)))
            .ToList();
        var last = await db.PostalCodes.AsNoTracking()
            .Select(p => (DateTimeOffset?)(p.ModifiedAt ?? p.CreatedAt))
            .MaxAsync(ct);
        var codes = await db.PostalCodes.AsNoTracking().Where(p => p.Active).Select(p => p.Code).Distinct().CountAsync(ct);
        return new PostalCodeSummaryDto(states.Sum(s => s.Settlements), codes, states.OrderBy(s => s.State).ToList(), last);
    }

    /// <summary>
    /// Adds and updates the settlements of the file. For the states the file covers, settlements that are no longer
    /// in it get a baja lógica (nothing is deleted). Importing the same file twice changes nothing.
    /// Administrators only (checked by the endpoint).
    /// </summary>
    public async Task<PostalCodeImportDto> ImportAsync(Stream file, CancellationToken ct)
    {
        // Last row wins if the file repeats a settlement.
        var incoming = SepomexFile.Read(file)
            .GroupBy(SepomexFile.Key)
            .ToDictionary(g => g.Key, g => g.Last());
        var stateCodes = incoming.Values.Select(p => p.StateCode).Distinct().ToList();

        // Not tracked: only the rows that change are attached, so a file of the whole country stays fast.
        var existing = (await db.PostalCodes.AsNoTracking().Where(p => stateCodes.Contains(p.StateCode)).ToListAsync(ct))
            .GroupBy(SepomexFile.Key)
            .ToDictionary(g => g.Key, g => g.First());

        int updated = 0, deactivated = 0;
        var now = clock.UtcNow;
        var newRows = new List<PostalCode>();
        foreach (var (key, row) in incoming)
        {
            if (!existing.TryGetValue(key, out var current))
            {
                // Inserted in bulk below, so the audit fields are set here.
                row.CreatedAt = now;
                row.CreatedBy = currentUser.Id;
                newRows.Add(row);
                continue;
            }
            if (Differs(current, row))
            {
                db.PostalCodes.Attach(current);
                current.Code = row.Code;
                current.Settlement = row.Settlement;
                current.SettlementType = row.SettlementType;
                current.Borough = row.Borough;
                current.City = row.City;
                current.State = row.State;
                current.AreaType = row.AreaType;
                current.Active = true;
                updated++;
            }
        }
        foreach (var (key, current) in existing)
        {
            if (current.Active && !incoming.ContainsKey(key))
            {
                db.PostalCodes.Attach(current);
                current.Active = false;
                deactivated++;
            }
        }

        await db.SaveChangesAsync(ct);
        await bulk.InsertAsync(newRows, ct);
        return new PostalCodeImportDto(
            incoming.Count, newRows.Count, updated, deactivated,
            incoming.Values.Select(p => p.State).Distinct().Order().ToList());
    }

    private static bool Differs(PostalCode a, PostalCode b) =>
        !a.Active || a.Code != b.Code || a.Settlement != b.Settlement || a.SettlementType != b.SettlementType
        || a.Borough != b.Borough || a.City != b.City || a.State != b.State || a.AreaType != b.AreaType;
}
