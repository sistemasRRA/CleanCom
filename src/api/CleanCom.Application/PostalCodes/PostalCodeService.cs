using CleanCom.Application.Common;
using CleanCom.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.PostalCodes;

/// <summary>
/// Official postal code catalog (Correos de México). Imported from its TXT export; used to fill colonia and alcaldía.
/// </summary>
public class PostalCodeService(ICleanComDbContext db)
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
        var states = await db.PostalCodes.AsNoTracking()
            .Where(p => p.Active)
            .GroupBy(p => p.State)
            .Select(g => new PostalCodeStateDto(g.Key, g.Count(), g.Select(p => p.Code).Distinct().Count()))
            .ToListAsync(ct);
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

        var existing = (await db.PostalCodes.Where(p => stateCodes.Contains(p.StateCode)).ToListAsync(ct))
            .GroupBy(SepomexFile.Key)
            .ToDictionary(g => g.Key, g => g.First());

        int added = 0, updated = 0, deactivated = 0;
        foreach (var (key, row) in incoming)
        {
            if (!existing.TryGetValue(key, out var current))
            {
                db.PostalCodes.Add(row);
                added++;
                continue;
            }
            if (Differs(current, row))
            {
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
                current.Active = false;
                deactivated++;
            }
        }

        await db.SaveChangesAsync(ct);
        return new PostalCodeImportDto(
            incoming.Count, added, updated, deactivated,
            incoming.Values.Select(p => p.State).Distinct().Order().ToList());
    }

    private static bool Differs(PostalCode a, PostalCode b) =>
        !a.Active || a.Code != b.Code || a.Settlement != b.Settlement || a.SettlementType != b.SettlementType
        || a.Borough != b.Borough || a.City != b.City || a.State != b.State || a.AreaType != b.AreaType;
}
