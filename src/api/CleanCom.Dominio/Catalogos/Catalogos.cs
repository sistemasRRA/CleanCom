using CleanCom.Dominio.Comun;
using NetTopologySuite.Geometries;

namespace CleanCom.Dominio.Catalogos;

/// <summary>
/// Código postal cubierto y su zona. Base para dibujar los polígonos.
/// </summary>
public class CoberturaCP : Entidad
{
    public string CodigoPostal { get; set; } = "";
    public string Colonia { get; set; } = "";
    public string Alcaldia { get; set; } = "";
    public int ZonaId { get; set; }
    public Zona? Zona { get; set; }
}

public enum TipoLugar
{
    Base,
    Bodega,
    Tienda,
}

/// <summary>
/// Lugar propio: la base (Leones), bodegas y tiendas 7clean.
/// </summary>
public class Lugar : Entidad, IActivable
{
    public string Nombre { get; set; } = "";
    public TipoLugar Tipo { get; set; }
    public string Direccion { get; set; } = "";
    /// <summary>Punto en SRID 4326 (<c>geography</c> en SQL Server).</summary>
    public Point? Ubicacion { get; set; }
    public string? Horario { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>
/// Tipo de parada editable (Convenio, Insumos, Traspaso).
/// </summary>
public class TipoParada : Entidad, IActivable
{
    public string Nombre { get; set; } = "";
    /// <summary>Campos que la parada debe capturar, separados por coma.</summary>
    public string? CamposRequeridos { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>
/// Tintorería, Lavandería, Planchaduría, Sastrería, Calzado.
/// </summary>
public class LineaServicio : Entidad, IActivable
{
    public string Nombre { get; set; } = "";
    /// <summary>Días de proceso prometidos; si es null se usa Planta.DiasProceso.</summary>
    public int? DiasProceso { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>
/// Día en que no se trabaja; la agenda lo salta.
/// </summary>
public class DiaInhabil : Entidad
{
    public DateOnly Fecha { get; set; }
    public string Motivo { get; set; } = "";
}

public enum TipoMotivo
{
    NoEntrega,
    AtrasoPlanta,
    ResultadoContacto,
}

public class MotivoCatalogo : Entidad, IActivable
{
    public TipoMotivo Tipo { get; set; }
    public string Texto { get; set; } = "";
    public bool Activo { get; set; } = true;
}
