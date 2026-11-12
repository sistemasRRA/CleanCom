using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using NetTopologySuite.Geometries;

namespace CleanCom.Domain.People;

/// <summary>
/// An invitado's address. Its zona is found by coordinates; the office may set it by hand.
/// </summary>
public class Direccion : Entity, IActivatable
{
    public int InvitadoId { get; set; }
    public Invitado? Invitado { get; set; }
    /// <summary>"Casa", "Oficina".</summary>
    public string? Label { get; set; }
    public string Street { get; set; } = "";
    public string? ExteriorNumber { get; set; }
    public string? InteriorNumber { get; set; }
    public string PostalCode { get; set; } = "";
    /// <summary>Colonia.</summary>
    public string? Neighborhood { get; set; }
    /// <summary>Alcaldía.</summary>
    public string? Borough { get; set; }
    public string? References { get; set; }
    /// <summary>Point in SRID 4326 (<c>geography</c> in SQL Server).</summary>
    public Point? Location { get; set; }
    public int? ZonaId { get; set; }
    public Zona? Zona { get; set; }
    /// <summary>The office chose the zona (ZonaAsignadaManual); it is not recalculated.</summary>
    public bool ZonaAssignedManually { get; set; }
    public bool Active { get; set; } = true;
}
