namespace CleanCom.Domain.Time;

/// <summary>
/// Conversions between UTC and business time (America/Mexico_City).
/// </summary>
public static class BusinessTime
{
    public const string TimeZoneId = "America/Mexico_City";

    public static TimeZoneInfo TimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    /// <summary>Converts an instant to Mexico City time.</summary>
    public static DateTimeOffset ToLocal(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, TimeZone);

    /// <summary>Business date (Mexico City) of an instant.</summary>
    public static DateOnly Date(DateTimeOffset instant) => DateOnly.FromDateTime(ToLocal(instant).DateTime);

    /// <summary>Business time of day (Mexico City) of an instant.</summary>
    public static TimeOnly TimeOfDay(DateTimeOffset instant) => TimeOnly.FromDateTime(ToLocal(instant).DateTime);

    /// <summary>UTC instant for a Mexico City date and time.</summary>
    public static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        var offset = TimeZone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }
}
