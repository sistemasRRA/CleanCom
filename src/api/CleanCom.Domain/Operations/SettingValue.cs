using System.Globalization;
using CleanCom.Domain.Common;

namespace CleanCom.Domain.Operations;

public readonly record struct TimeRange(TimeOnly Start, TimeOnly End);

/// <summary>
/// Validates, normalizes and reads setting values according to their type.
/// </summary>
public static class SettingValue
{
    private const string TimeFormat = "HH:mm";
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>
    /// Returns the value in its canonical form or throws <see cref="DomainException"/> with the reason.
    /// </summary>
    public static string Normalize(SettingValueType type, string? value)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0)
        {
            throw new DomainException("El valor no puede ir vacío.");
        }

        return type switch
        {
            SettingValueType.Text => text,
            SettingValueType.Integer => AsInteger(text).ToString(Invariant),
            SettingValueType.Decimal => AsDecimal(text).ToString(Invariant),
            SettingValueType.Boolean => AsBoolean(text) ? "true" : "false",
            SettingValueType.Time => AsTime(text).ToString(TimeFormat, Invariant),
            SettingValueType.TimeRange => FormatRange(AsTimeRange(text)),
            SettingValueType.Coordinate => FormatCoordinate(AsCoordinate(text)),
            _ => throw new DomainException($"Tipo de valor no soportado: {type}."),
        };
    }

    public static int AsInteger(string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, Invariant, out var n) || n < 0)
        {
            throw new DomainException($"\"{value}\" no es un número entero válido.");
        }
        return n;
    }

    public static decimal AsDecimal(string value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, Invariant, out var n) || n < 0)
        {
            throw new DomainException($"\"{value}\" no es un número válido.");
        }
        return n;
    }

    /// <summary>Accepts the Spanish "sí"/"no" typed on the Operación screen as well as true/false.</summary>
    public static bool AsBoolean(string value) => value.Trim().ToLowerInvariant() switch
    {
        "true" or "si" or "sí" or "1" => true,
        "false" or "no" or "0" => false,
        _ => throw new DomainException($"\"{value}\" no es sí o no."),
    };

    public static TimeOnly AsTime(string value)
    {
        if (!TimeOnly.TryParseExact(value.Trim(), ["H:mm", "HH:mm"], Invariant, DateTimeStyles.None, out var time))
        {
            throw new DomainException($"\"{value}\" no es una hora válida (HH:mm).");
        }
        return time;
    }

    public static TimeRange AsTimeRange(string value)
    {
        var parts = value.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new DomainException($"\"{value}\" no es un rango válido (HH:mm-HH:mm).");
        }
        var range = new TimeRange(AsTime(parts[0]), AsTime(parts[1]));
        if (range.Start >= range.End)
        {
            throw new DomainException("La hora de inicio debe ser anterior a la hora de fin.");
        }
        return range;
    }

    public static (double Lat, double Lng) AsCoordinate(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !double.TryParse(parts[0], NumberStyles.Float, Invariant, out var lat)
            || !double.TryParse(parts[1], NumberStyles.Float, Invariant, out var lng)
            || lat is < -90 or > 90
            || lng is < -180 or > 180)
        {
            throw new DomainException($"\"{value}\" no es una coordenada válida (latitud,longitud).");
        }
        return (lat, lng);
    }

    private static string FormatRange(TimeRange r) =>
        $"{r.Start.ToString(TimeFormat, Invariant)}-{r.End.ToString(TimeFormat, Invariant)}";

    private static string FormatCoordinate((double Lat, double Lng) c) =>
        $"{c.Lat.ToString(Invariant)},{c.Lng.ToString(Invariant)}";
}
