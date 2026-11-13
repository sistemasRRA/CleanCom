using CleanCom.Application.Common;
using CleanCom.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace CleanCom.Application.Agenda;

/// <summary>How the zona of an address was found.</summary>
public enum ZonaSource
{
    /// <summary>The coordinates fall inside the zona polygon.</summary>
    Poligono,
    /// <summary>By postal code, while the zona has no polygon drawn yet.</summary>
    CodigoPostal,
    /// <summary>Chosen by the office (ZonaAsignadaManual).</summary>
    Manual,
    /// <summary>No zona found: the office chooses it by hand.</summary>
    FueraDeCobertura,
}

public record ZonaLocation(Zona? Zona, ZonaSource Source, string Explanation);

/// <summary>
/// Finds the zona of an address (docs/03-reglas-de-negocio.md, Zonas 3 and 4): first by polygon; for zonas without
/// a polygon yet, by postal code (PostalCodeCoverage); otherwise it is out of coverage and the office picks the zona.
/// The "most feasible ruta" proposal for out-of-coverage addresses arrives with Google routing (week 3).
/// </summary>
public class ZonaLocator(ICleanComDbContext db)
{
    public async Task<ZonaLocation> LocateAsync(Point? location, string? postalCode, string? neighborhood, CancellationToken ct)
    {
        if (location is not null)
        {
            var inside = await Zonas()
                .Where(z => z.Polygon != null && z.Polygon.Contains(location))
                .ToListAsync(ct);
            if (inside.Count == 1)
            {
                return new(inside[0], ZonaSource.Poligono, $"Zona {inside[0].Code} por la ubicación de la dirección.");
            }
            if (inside.Count > 1)
            {
                return new(null, ZonaSource.FueraDeCobertura,
                    $"La dirección cae en el borde de las zonas {string.Join(" y ", inside.Select(z => z.Code))}: elige la zona a mano.");
            }
        }

        var cp = postalCode?.Trim();
        if (!string.IsNullOrEmpty(cp))
        {
            var rows = await db.Coverage.AsNoTracking()
                .Where(c => c.Active && c.PostalCode == cp && c.Zona!.Active && c.Zona.Polygon == null)
                .Select(c => new { c.ZonaId, c.Neighborhood })
                .ToListAsync(ct);
            var zonaIds = rows.Select(r => r.ZonaId).Distinct().ToList();

            // A postal code split between zonas is decided by its colonia.
            if (zonaIds.Count > 1 && !string.IsNullOrWhiteSpace(neighborhood))
            {
                var colonia = neighborhood.Trim();
                zonaIds = rows.Where(r => string.Equals(r.Neighborhood, colonia, StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.ZonaId).Distinct().ToList();
            }
            if (zonaIds.Count == 1)
            {
                var zona = await Zonas().FirstAsync(z => z.Id == zonaIds[0], ct);
                return new(zona, ZonaSource.CodigoPostal, $"Zona {zona.Code} por el código postal {cp} (la zona aún no tiene polígono).");
            }
            if (zonaIds.Count > 1)
            {
                return new(null, ZonaSource.FueraDeCobertura, $"El código postal {cp} pertenece a varias zonas: elige la zona a mano.");
            }
        }

        return new(null, ZonaSource.FueraDeCobertura, "La dirección está fuera de cobertura: elige la zona a mano.");
    }

    public async Task<ZonaLocation> ManualAsync(int zonaId, CancellationToken ct)
    {
        var zona = await Zonas().FirstOrDefaultAsync(z => z.Id == zonaId, ct)
            ?? throw new NotFoundException($"La zona {zonaId} no existe o está inactiva.");
        return new(zona, ZonaSource.Manual, $"Zona {zona.Code} asignada a mano.");
    }

    /// <summary>Active zonas with what the agenda needs: ruta and visit days (the núcleo's for EXT zonas).</summary>
    private IQueryable<Zona> Zonas() => db.Zonas.AsNoTracking()
        .Where(z => z.Active)
        .Include(z => z.Ruta)
        .Include(z => z.VisitDays)
        .Include(z => z.ZonaNucleo!).ThenInclude(n => n.VisitDays);
}
