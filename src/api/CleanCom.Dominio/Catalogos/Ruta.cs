using System.Globalization;
using CleanCom.Dominio.Comun;
using CleanCom.Dominio.Operacion;

namespace CleanCom.Dominio.Catalogos;

/// <summary>
/// Recorrido diario de una unidad (F, K). Los valores opcionales sobrescriben la configuración general.
/// </summary>
public class Ruta : Entidad, IActivable
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    /// <summary>Color en hexadecimal (#RRGGBB), igual en todas las pantallas.</summary>
    public string Color { get; set; } = "";
    public int? CupoParadas { get; set; }
    public int? MetaRecolecciones { get; set; }
    public TimeOnly? RegresoMaximo { get; set; }
    public TimeOnly? RegresoMaximoSabado { get; set; }
    public bool Activo { get; set; } = true;

    public List<Zona> Zonas { get; set; } = [];

    /// <summary>
    /// Valor propio de la ruta para una clave de configuración, o null si usa el general.
    /// </summary>
    public string? ValorPropio(string clave) => clave switch
    {
        ClavesConfiguracion.RutaCupoParadas => CupoParadas?.ToString(CultureInfo.InvariantCulture),
        ClavesConfiguracion.RutaMetaRecolecciones => MetaRecolecciones?.ToString(CultureInfo.InvariantCulture),
        ClavesConfiguracion.RutaRegresoMaximo => RegresoMaximo?.ToString("HH:mm", CultureInfo.InvariantCulture),
        ClavesConfiguracion.RutaRegresoMaximoSabado => RegresoMaximoSabado?.ToString("HH:mm", CultureInfo.InvariantCulture),
        _ => null,
    };
}
