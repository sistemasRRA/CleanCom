using CleanCom.Dominio.Catalogos;
using CleanCom.Dominio.Comun;
using CleanCom.Dominio.Geografia;
using NetTopologySuite.Algorithm;

namespace CleanCom.Pruebas.Catalogos;

/// <summary>
/// docs/03-reglas-de-negocio.md, sección Zonas.
/// </summary>
public class ZonaTests
{
    private static Zona ZonaCon(int id, string codigo, int rutaId, params DayOfWeek[] dias) => new()
    {
        Id = id,
        Codigo = codigo,
        RutaId = rutaId,
        DiasVisita = dias.Select(d => new ZonaDiaVisita { ZonaId = id, DiaSemana = d }).ToList(),
    };

    [Fact]
    public void Zona_1_va_lunes_y_jueves()
    {
        var k1 = ZonaCon(1, "K1", 2, DayOfWeek.Thursday, DayOfWeek.Monday);

        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Thursday], k1.DiasDeVisitaEfectivos());
    }

    [Fact]
    public void Una_zona_EXT_sigue_los_dias_de_su_zona_nucleo()
    {
        var k1 = ZonaCon(1, "K1", 2, DayOfWeek.Monday, DayOfWeek.Thursday);
        // Aunque tenga días propios capturados por error, manda el núcleo.
        var k1Ext = ZonaCon(7, "K1EXT", 2, DayOfWeek.Saturday);
        k1Ext.ZonaNucleoId = k1.Id;
        k1Ext.ZonaNucleo = k1;

        Assert.True(k1Ext.EsExtension);
        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Thursday], k1Ext.DiasDeVisitaEfectivos());
    }

    [Fact]
    public void La_zona_nucleo_debe_ser_de_la_misma_ruta()
    {
        var f1 = ZonaCon(1, "F1", 1);
        var k1Ext = ZonaCon(7, "K1EXT", 2);

        Assert.Throws<ExcepcionDominio>(() => k1Ext.ValidarNucleo(f1));
    }

    [Fact]
    public void Una_zona_EXT_no_puede_ser_nucleo_de_otra()
    {
        var k1Ext = ZonaCon(7, "K1EXT", 2);
        k1Ext.ZonaNucleoId = 1;
        var otra = ZonaCon(8, "K1EXT2", 2);

        Assert.Throws<ExcepcionDominio>(() => otra.ValidarNucleo(k1Ext));
    }
}

public class GeoTests
{
    [Fact]
    public void El_punto_guarda_longitud_como_X_y_latitud_como_Y()
    {
        var leones = Geo.Punto(19.3608092, -99.1968494);

        Assert.Equal(4326, leones.SRID);
        Assert.Equal(-99.1968494, leones.X);
        Assert.Equal(19.3608092, leones.Y);
    }

    [Fact]
    public void El_poligono_se_cierra_y_queda_en_sentido_contrario_a_las_manecillas()
    {
        // Capturado en el sentido de las manecillas del reloj, sin cerrar.
        var vertices = new[]
        {
            new Vertice(19.40, -99.20),
            new Vertice(19.40, -99.10),
            new Vertice(19.30, -99.10),
            new Vertice(19.30, -99.20),
        };

        var poligono = Geo.Poligono(vertices);

        Assert.True(poligono.ExteriorRing.IsClosed);
        Assert.True(Orientation.IsCCW(poligono.ExteriorRing.CoordinateSequence));
        Assert.True(poligono.Contains(Geo.Punto(19.35, -99.15)));
    }

    [Fact]
    public void Un_poligono_que_se_cruza_es_invalido()
    {
        var moño = new[]
        {
            new Vertice(19.40, -99.20),
            new Vertice(19.30, -99.10),
            new Vertice(19.40, -99.10),
            new Vertice(19.30, -99.20),
        };

        Assert.Throws<ExcepcionDominio>(() => Geo.Poligono(moño));
    }

    [Fact]
    public void Un_poligono_necesita_tres_vertices()
    {
        Assert.Throws<ExcepcionDominio>(() => Geo.Poligono([new Vertice(19.4, -99.2), new Vertice(19.3, -99.1)]));
    }
}
