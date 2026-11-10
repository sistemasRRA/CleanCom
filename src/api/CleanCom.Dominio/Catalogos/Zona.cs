using CleanCom.Dominio.Comun;
using NetTopologySuite.Geometries;

namespace CleanCom.Dominio.Catalogos;

/// <summary>
/// Área geográfica de una ruta (F1, K3, K1EXT). Las zonas EXT siguen los días de visita de su zona núcleo.
/// </summary>
public class Zona : Entidad, IActivable
{
    public string Codigo { get; set; } = "";
    public int RutaId { get; set; }
    public Ruta? Ruta { get; set; }
    public int? ZonaNucleoId { get; set; }
    public Zona? ZonaNucleo { get; set; }
    /// <summary>Polígono en SRID 4326 (<c>geography</c> en SQL Server).</summary>
    public Polygon? Poligono { get; set; }
    public bool Activo { get; set; } = true;

    public List<ZonaDiaVisita> DiasVisita { get; set; } = [];

    public bool EsExtension => ZonaNucleoId is not null;

    /// <summary>
    /// Días en que pasa la ruta: los de la zona núcleo si es EXT, si no los propios.
    /// Requiere tener cargada <see cref="ZonaNucleo"/> con sus días cuando es EXT.
    /// </summary>
    public IReadOnlyList<DayOfWeek> DiasDeVisitaEfectivos()
    {
        if (EsExtension)
        {
            if (ZonaNucleo is null)
            {
                throw new InvalidOperationException($"La zona {Codigo} es EXT y no se cargó su zona núcleo.");
            }
            return ZonaNucleo.DiasDeVisitaEfectivos();
        }

        return DiasVisita.Select(d => d.DiaSemana).Distinct().Order().ToList();
    }

    /// <summary>
    /// Valida que <paramref name="nucleo"/> pueda ser la zona núcleo de esta zona.
    /// </summary>
    public void ValidarNucleo(Zona nucleo)
    {
        if (nucleo.Id == Id && Id != 0)
        {
            throw new ExcepcionDominio("Una zona no puede ser su propia zona núcleo.");
        }
        if (nucleo.EsExtension)
        {
            throw new ExcepcionDominio($"La zona {nucleo.Codigo} es EXT; la zona núcleo debe ser una zona normal.");
        }
        if (nucleo.RutaId != RutaId)
        {
            throw new ExcepcionDominio("La zona EXT debe pertenecer a la misma ruta que su zona núcleo.");
        }
    }
}

public class ZonaDiaVisita : Entidad
{
    public int ZonaId { get; set; }
    public DayOfWeek DiaSemana { get; set; }
}
