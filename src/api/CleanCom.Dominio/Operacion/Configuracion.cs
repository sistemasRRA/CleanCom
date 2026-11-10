using CleanCom.Dominio.Comun;

namespace CleanCom.Dominio.Operacion;

public enum TipoValorConfiguracion
{
    Texto,
    Entero,
    Decimal,
    Booleano,
    /// <summary>Hora del día, "HH:mm".</summary>
    Hora,
    /// <summary>Rango de horas, "HH:mm-HH:mm".</summary>
    RangoHoras,
    /// <summary>Latitud y longitud, "19.36,-99.19".</summary>
    Coordenada,
}

/// <summary>
/// Un valor configurable de Operación. Cada cambio es un registro nuevo: así queda el historial
/// y se pueden programar cambios con <see cref="VigenteDesde"/> en el futuro.
/// </summary>
public class Configuracion : Entidad
{
    public string Clave { get; set; } = "";
    public string Valor { get; set; } = "";
    public TipoValorConfiguracion Tipo { get; set; }
    public DateTimeOffset VigenteDesde { get; set; }
    public string? Nota { get; set; }
}
