using CleanCom.Dominio.Tiempo;
using CleanCom.Infraestructura.Tiempo;

namespace CleanCom.Pruebas.Tiempo;

/// <summary>TimeProvider con la hora que pida la prueba.</summary>
public sealed class HoraFija(DateTimeOffset utc) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utc;

    /// <summary>Hora fija a partir de una hora de Ciudad de México.</summary>
    public static HoraFija EnCdmx(int anio, int mes, int dia, int hora, int minuto) =>
        new(HoraNegocio.AUtc(new DateOnly(anio, mes, dia), new TimeOnly(hora, minuto)));
}

public class RelojNegocioTests
{
    [Fact]
    public void Ciudad_de_Mexico_esta_en_UTC_menos_6_todo_el_anio()
    {
        // Desde 2022 no hay horario de verano en Ciudad de México.
        var invierno = HoraNegocio.AUtc(new DateOnly(2026, 1, 15), new TimeOnly(13, 0));
        var verano = HoraNegocio.AUtc(new DateOnly(2026, 7, 15), new TimeOnly(13, 0));

        Assert.Equal(new DateTimeOffset(2026, 1, 15, 19, 0, 0, TimeSpan.Zero), invierno);
        Assert.Equal(new DateTimeOffset(2026, 7, 15, 19, 0, 0, TimeSpan.Zero), verano);
    }

    [Fact]
    public void La_fecha_de_negocio_es_la_de_Ciudad_de_Mexico_no_la_UTC()
    {
        // 2 de noviembre a las 23:30 en CDMX ya es 3 de noviembre en UTC.
        var reloj = new RelojNegocio(HoraFija.EnCdmx(2026, 11, 2, 23, 30));

        Assert.Equal(new DateOnly(2026, 11, 3), DateOnly.FromDateTime(reloj.AhoraUtc.UtcDateTime));
        Assert.Equal(new DateOnly(2026, 11, 2), reloj.Hoy);
        Assert.Equal(new TimeOnly(23, 30), TimeOnly.FromDateTime(reloj.AhoraLocal.DateTime));
        Assert.Equal(TimeSpan.FromHours(-6), reloj.AhoraLocal.Offset);
    }

    [Fact]
    public void AhoraUtc_se_guarda_con_desfase_cero()
    {
        var reloj = new RelojNegocio(HoraFija.EnCdmx(2026, 11, 2, 12, 1));

        Assert.Equal(TimeSpan.Zero, reloj.AhoraUtc.Offset);
        Assert.Equal(new TimeOnly(12, 1), HoraNegocio.Hora(reloj.AhoraUtc));
    }
}
