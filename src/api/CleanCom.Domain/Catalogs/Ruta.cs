using System.Globalization;
using CleanCom.Domain.Common;
using CleanCom.Domain.Operations;

namespace CleanCom.Domain.Catalogs;

/// <summary>
/// Daily trip of a unidad (F, K). Optional values override the general settings.
/// </summary>
public class Ruta : Entity, IActivatable
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Hex color (#RRGGBB), the same on every screen.</summary>
    public string Color { get; set; } = "";
    public int? CupoParadas { get; set; }
    public int? RecoleccionGoal { get; set; }
    public TimeOnly? MaxReturnTime { get; set; }
    public TimeOnly? MaxReturnTimeSaturday { get; set; }
    public bool Active { get; set; } = true;

    public List<Zona> Zonas { get; set; } = [];

    /// <summary>
    /// The ruta's own value for a setting key, or null when it uses the general one.
    /// </summary>
    public string? OwnValue(string key) => key switch
    {
        SettingKeys.RutaCupoParadas => CupoParadas?.ToString(CultureInfo.InvariantCulture),
        SettingKeys.RutaRecoleccionGoal => RecoleccionGoal?.ToString(CultureInfo.InvariantCulture),
        SettingKeys.RutaMaxReturnTime => MaxReturnTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
        SettingKeys.RutaMaxReturnTimeSaturday => MaxReturnTimeSaturday?.ToString("HH:mm", CultureInfo.InvariantCulture),
        _ => null,
    };
}
