using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;

namespace CleanCom.Domain.Operations;

/// <summary>
/// Decides which setting value applies at a given moment.
/// </summary>
public static class SettingResolver
{
    /// <summary>
    /// The current row: the latest <see cref="Setting.EffectiveFrom"/> that has already started.
    /// Changes scheduled in the future do not apply until their date.
    /// </summary>
    public static Setting? Current(IEnumerable<Setting> history, string key, DateTimeOffset utcNow) =>
        history
            .Where(s => s.Key == key && s.EffectiveFrom <= utcNow)
            .OrderByDescending(s => s.EffectiveFrom)
            .ThenByDescending(s => s.Id)
            .FirstOrDefault();

    /// <summary>
    /// The next scheduled change, if any.
    /// </summary>
    public static Setting? Scheduled(IEnumerable<Setting> history, string key, DateTimeOffset utcNow) =>
        history
            .Where(s => s.Key == key && s.EffectiveFrom > utcNow)
            .OrderBy(s => s.EffectiveFrom)
            .ThenByDescending(s => s.Id)
            .FirstOrDefault();

    /// <summary>
    /// The value that applies. When the key can be set per ruta and the ruta has its own value, the ruta wins.
    /// </summary>
    public static string Resolve(IEnumerable<Setting> history, string key, DateTimeOffset utcNow, Ruta? ruta = null)
    {
        var definition = SettingCatalog.Find(key)
            ?? throw new DomainException($"La clave de configuración \"{key}\" no existe.");

        if (definition.PerRuta && ruta?.OwnValue(key) is { } own)
        {
            return own;
        }

        return Current(history, key, utcNow)?.Value
            ?? throw new DomainException($"La configuración \"{key}\" no tiene valor vigente.");
    }
}
