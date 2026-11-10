using CleanCom.Dominio.Comun;
using NetTopologySuite;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;

namespace CleanCom.Dominio.Geografia;

public readonly record struct Vertice(double Lat, double Lng);

/// <summary>
/// Puntos y polígonos en WGS 84 (SRID 4326), compatibles con <c>geography</c> de SQL Server.
/// </summary>
public static class Geo
{
    public const int Srid = 4326;

    private static readonly GeometryFactory Fabrica = NtsGeometryServices.Instance.CreateGeometryFactory(Srid);

    public static Point Punto(double lat, double lng)
    {
        ValidarCoordenada(lat, lng);
        return Fabrica.CreatePoint(new Coordinate(lng, lat));
    }

    /// <summary>
    /// Crea un polígono cerrado. El anillo se orienta en sentido contrario a las manecillas del reloj,
    /// como lo exige <c>geography</c>; si se guardara al revés, SQL Server lo tomaría como "todo el mundo menos la zona".
    /// </summary>
    public static Polygon Poligono(IReadOnlyList<Vertice> vertices)
    {
        if (vertices.Count < 3)
        {
            throw new ExcepcionDominio("El polígono necesita al menos 3 vértices.");
        }
        foreach (var v in vertices)
        {
            ValidarCoordenada(v.Lat, v.Lng);
        }

        var coordenadas = vertices.Select(v => new Coordinate(v.Lng, v.Lat)).ToList();
        if (!coordenadas[0].Equals2D(coordenadas[^1]))
        {
            coordenadas.Add(coordenadas[0].Copy());
        }
        if (coordenadas.Count < 4)
        {
            throw new ExcepcionDominio("El polígono necesita al menos 3 vértices distintos.");
        }

        var anillo = coordenadas.ToArray();
        if (!Orientation.IsCCW(anillo))
        {
            Array.Reverse(anillo);
        }

        var poligono = Fabrica.CreatePolygon(anillo);
        if (!poligono.IsValid)
        {
            throw new ExcepcionDominio("El polígono no es válido: sus lados se cruzan.");
        }
        return poligono;
    }

    public static IReadOnlyList<Vertice> Vertices(Polygon poligono) =>
        poligono.ExteriorRing.Coordinates.Select(c => new Vertice(c.Y, c.X)).ToList();

    private static void ValidarCoordenada(double lat, double lng)
    {
        if (lat is < -90 or > 90 || lng is < -180 or > 180 || double.IsNaN(lat) || double.IsNaN(lng))
        {
            throw new ExcepcionDominio($"La coordenada ({lat}, {lng}) no es válida.");
        }
    }
}
