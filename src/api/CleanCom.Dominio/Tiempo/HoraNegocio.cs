namespace CleanCom.Dominio.Tiempo;

/// <summary>
/// Conversiones entre UTC y la hora de negocio (America/Mexico_City).
/// </summary>
public static class HoraNegocio
{
    public const string IdZonaHoraria = "America/Mexico_City";

    public static TimeZoneInfo ZonaHoraria { get; } = TimeZoneInfo.FindSystemTimeZoneById(IdZonaHoraria);

    /// <summary>Convierte un instante a la hora de Ciudad de México.</summary>
    public static DateTimeOffset ALocal(DateTimeOffset instante) => TimeZoneInfo.ConvertTime(instante, ZonaHoraria);

    /// <summary>Fecha de negocio (Ciudad de México) de un instante.</summary>
    public static DateOnly Fecha(DateTimeOffset instante) => DateOnly.FromDateTime(ALocal(instante).DateTime);

    /// <summary>Hora de negocio (Ciudad de México) de un instante.</summary>
    public static TimeOnly Hora(DateTimeOffset instante) => TimeOnly.FromDateTime(ALocal(instante).DateTime);

    /// <summary>Instante UTC que corresponde a una fecha y hora de Ciudad de México.</summary>
    public static DateTimeOffset AUtc(DateOnly fecha, TimeOnly hora)
    {
        var local = fecha.ToDateTime(hora, DateTimeKind.Unspecified);
        var desfase = ZonaHoraria.GetUtcOffset(local);
        return new DateTimeOffset(local, desfase).ToUniversalTime();
    }
}
