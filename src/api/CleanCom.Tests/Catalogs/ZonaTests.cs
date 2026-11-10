using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Geography;
using NetTopologySuite.Algorithm;

namespace CleanCom.Tests.Catalogs;

/// <summary>
/// docs/03-reglas-de-negocio.md, Zonas section.
/// </summary>
public class ZonaTests
{
    private static Zona ZonaWith(int id, string code, int rutaId, params DayOfWeek[] days) => new()
    {
        Id = id,
        Code = code,
        RutaId = rutaId,
        VisitDays = days.Select(d => new ZonaVisitDay { ZonaId = id, DayOfWeek = d }).ToList(),
    };

    [Fact]
    public void Zona_1_is_visited_monday_and_thursday()
    {
        var k1 = ZonaWith(1, "K1", 2, DayOfWeek.Thursday, DayOfWeek.Monday);
        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Thursday], k1.EffectiveVisitDays());
    }

    [Fact]
    public void An_EXT_zona_follows_the_days_of_its_zona_nucleo()
    {
        var k1 = ZonaWith(1, "K1", 2, DayOfWeek.Monday, DayOfWeek.Thursday);
        // Even with days of its own captured by mistake, the núcleo wins.
        var k1Ext = ZonaWith(7, "K1EXT", 2, DayOfWeek.Saturday);
        k1Ext.ZonaNucleoId = k1.Id;
        k1Ext.ZonaNucleo = k1;

        Assert.True(k1Ext.IsExtension);
        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Thursday], k1Ext.EffectiveVisitDays());
    }

    [Fact]
    public void The_zona_nucleo_must_belong_to_the_same_ruta()
    {
        var f1 = ZonaWith(1, "F1", 1);
        var k1Ext = ZonaWith(7, "K1EXT", 2);
        Assert.Throws<DomainException>(() => k1Ext.ValidateNucleo(f1));
    }

    [Fact]
    public void An_EXT_zona_cannot_be_the_nucleo_of_another()
    {
        var k1Ext = ZonaWith(7, "K1EXT", 2);
        k1Ext.ZonaNucleoId = 1;
        var other = ZonaWith(8, "K1EXT2", 2);
        Assert.Throws<DomainException>(() => other.ValidateNucleo(k1Ext));
    }
}

public class GeoTests
{
    [Fact]
    public void The_point_stores_longitude_as_X_and_latitude_as_Y()
    {
        var leones = Geo.CreatePoint(19.3608092, -99.1968494);
        Assert.Equal(4326, leones.SRID);
        Assert.Equal(-99.1968494, leones.X);
        Assert.Equal(19.3608092, leones.Y);
    }

    [Fact]
    public void The_polygon_is_closed_and_counterclockwise()
    {
        // Captured clockwise, not closed.
        var vertices = new[]
        {
            new Vertex(19.40, -99.20),
            new Vertex(19.40, -99.10),
            new Vertex(19.30, -99.10),
            new Vertex(19.30, -99.20),
        };
        var polygon = Geo.CreatePolygon(vertices);

        Assert.True(polygon.ExteriorRing.IsClosed);
        Assert.True(Orientation.IsCCW(polygon.ExteriorRing.CoordinateSequence));
        Assert.True(polygon.Contains(Geo.CreatePoint(19.35, -99.15)));
    }

    [Fact]
    public void A_self_crossing_polygon_is_invalid()
    {
        var bowTie = new[]
        {
            new Vertex(19.40, -99.20),
            new Vertex(19.30, -99.10),
            new Vertex(19.40, -99.10),
            new Vertex(19.30, -99.20),
        };
        Assert.Throws<DomainException>(() => Geo.CreatePolygon(bowTie));
    }

    [Fact]
    public void A_polygon_needs_three_vertices()
    {
        Assert.Throws<DomainException>(() => Geo.CreatePolygon([new Vertex(19.4, -99.2), new Vertex(19.3, -99.1)]));
    }
}
