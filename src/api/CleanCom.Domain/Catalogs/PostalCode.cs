using CleanCom.Domain.Common;

namespace CleanCom.Domain.Catalogs;

/// <summary>
/// A settlement (colonia, barrio, pueblo…) of the official postal code catalog of Correos de México (SEPOMEX).
/// Reference data to fill colonia and alcaldía when capturing an address; it does not decide the zona
/// (that is <see cref="PostalCodeCoverage"/>). Imported from the official file, never stored in the repository:
/// its terms forbid distributing it.
/// </summary>
public class PostalCode : Entity, IActivatable
{
    /// <summary>d_codigo.</summary>
    public string Code { get; set; } = "";
    /// <summary>d_asenta: the colonia name.</summary>
    public string Settlement { get; set; } = "";
    /// <summary>d_tipo_asenta: Colonia, Barrio, Pueblo, Unidad habitacional…</summary>
    public string SettlementType { get; set; } = "";
    /// <summary>D_mnpio: alcaldía or municipio.</summary>
    public string Borough { get; set; } = "";
    /// <summary>d_ciudad; empty outside cities.</summary>
    public string? City { get; set; }
    /// <summary>d_estado.</summary>
    public string State { get; set; } = "";
    /// <summary>c_estado: "09" for Ciudad de México.</summary>
    public string StateCode { get; set; } = "";
    /// <summary>c_mnpio.</summary>
    public string BoroughCode { get; set; } = "";
    /// <summary>id_asenta_cpcons: consecutive per municipio. With the state and municipio it identifies the settlement.</summary>
    public string SettlementId { get; set; } = "";
    /// <summary>d_zona: Urbano or Rural.</summary>
    public string? AreaType { get; set; }
    public bool Active { get; set; } = true;
}
