using CleanCom.Domain.Common;

namespace CleanCom.Domain.Operations;

public enum SettingValueType
{
    Text,
    Integer,
    Decimal,
    Boolean,
    /// <summary>Time of day, "HH:mm".</summary>
    Time,
    /// <summary>Time range, "HH:mm-HH:mm".</summary>
    TimeRange,
    /// <summary>Latitude and longitude, "19.36,-99.19".</summary>
    Coordinate,
}

/// <summary>
/// A configurable value of the Operación screen. Every change is a new row: that keeps the history
/// and allows scheduling changes with a future <see cref="EffectiveFrom"/>.
/// </summary>
public class Setting : Entity
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public SettingValueType Type { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public string? Note { get; set; }
}
