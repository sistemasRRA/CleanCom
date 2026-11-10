using CleanCom.Domain.Common;
using NetTopologySuite.Geometries;

namespace CleanCom.Domain.Catalogs;

/// <summary>
/// Geographic area of a ruta (F1, K3, K1EXT). EXT zonas follow the visit days of their zona núcleo.
/// </summary>
public class Zona : Entity, IActivatable
{
    public string Code { get; set; } = "";
    public int RutaId { get; set; }
    public Ruta? Ruta { get; set; }
    public int? ZonaNucleoId { get; set; }
    public Zona? ZonaNucleo { get; set; }
    /// <summary>Polygon in SRID 4326 (<c>geography</c> in SQL Server).</summary>
    public Polygon? Polygon { get; set; }
    public bool Active { get; set; } = true;

    public List<ZonaVisitDay> VisitDays { get; set; } = [];

    public bool IsExtension => ZonaNucleoId is not null;

    /// <summary>
    /// Days the ruta comes by: the zona núcleo's days for an EXT zona, otherwise its own.
    /// For an EXT zona, <see cref="ZonaNucleo"/> must be loaded with its days.
    /// </summary>
    public IReadOnlyList<DayOfWeek> EffectiveVisitDays()
    {
        if (IsExtension)
        {
            if (ZonaNucleo is null)
            {
                throw new InvalidOperationException($"Zona {Code} is EXT and its zona núcleo was not loaded.");
            }
            return ZonaNucleo.EffectiveVisitDays();
        }

        return VisitDays.Select(d => d.DayOfWeek).Distinct().Order().ToList();
    }

    /// <summary>
    /// Checks that <paramref name="nucleo"/> can be the zona núcleo of this zona.
    /// </summary>
    public void ValidateNucleo(Zona nucleo)
    {
        if (nucleo.Id == Id && Id != 0)
        {
            throw new DomainException("Una zona no puede ser su propia zona núcleo.");
        }
        if (nucleo.IsExtension)
        {
            throw new DomainException($"La zona {nucleo.Code} es EXT; la zona núcleo debe ser una zona normal.");
        }
        if (nucleo.RutaId != RutaId)
        {
            throw new DomainException("La zona EXT debe pertenecer a la misma ruta que su zona núcleo.");
        }
    }
}

public class ZonaVisitDay : Entity
{
    public int ZonaId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
}
