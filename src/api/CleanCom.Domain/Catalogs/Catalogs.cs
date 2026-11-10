using CleanCom.Domain.Common;
using NetTopologySuite.Geometries;

namespace CleanCom.Domain.Catalogs;

/// <summary>
/// Postal code and neighborhood served by a zona. Starting point to draw the zona polygons.
/// </summary>
public class PostalCodeCoverage : Entity
{
    public string PostalCode { get; set; } = "";
    /// <summary>Colonia.</summary>
    public string Neighborhood { get; set; } = "";
    /// <summary>Alcaldía.</summary>
    public string Borough { get; set; } = "";
    public int ZonaId { get; set; }
    public Zona? Zona { get; set; }
}

public enum LugarType
{
    Base,
    Bodega,
    Tienda,
}

/// <summary>
/// CleanCom's own place: the base (Leones), bodegas and 7clean tiendas.
/// </summary>
public class Lugar : Entity, IActivatable
{
    public string Name { get; set; } = "";
    public LugarType Type { get; set; }
    public string Address { get; set; } = "";
    /// <summary>Point in SRID 4326 (<c>geography</c> in SQL Server).</summary>
    public Point? Location { get; set; }
    public string? OpeningHours { get; set; }
    public bool Active { get; set; } = true;
}

/// <summary>
/// Editable parada type (Convenio, Insumos, Traspaso).
/// </summary>
public class ParadaType : Entity, IActivatable
{
    public string Name { get; set; } = "";
    /// <summary>Fields the parada must capture, comma separated.</summary>
    public string? RequiredFields { get; set; }
    public bool Active { get; set; } = true;
}

/// <summary>
/// Tintorería, Lavandería, Planchaduría, Sastrería, Calzado.
/// </summary>
public class LineaServicio : Entity, IActivatable
{
    public string Name { get; set; } = "";
    /// <summary>Promised processing days; when null, Planta.DiasProceso applies.</summary>
    public int? ProcessingDays { get; set; }
    public bool Active { get; set; } = true;
}

/// <summary>
/// Day off; scheduling skips it.
/// </summary>
public class NonWorkingDay : Entity
{
    public DateOnly Date { get; set; }
    public string Reason { get; set; } = "";
}

public enum ReasonType
{
    FailedEntrega,
    PlantaDelay,
    ContactResult,
}

/// <summary>
/// Catalog of reasons: failed entregas, planta delays and CRM contact results.
/// </summary>
public class Reason : Entity, IActivatable
{
    public ReasonType Type { get; set; }
    public string Text { get; set; } = "";
    public bool Active { get; set; } = true;
}
