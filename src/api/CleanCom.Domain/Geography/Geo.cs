using CleanCom.Domain.Common;
using NetTopologySuite;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;

namespace CleanCom.Domain.Geography;

public readonly record struct Vertex(double Lat, double Lng);

/// <summary>
/// Points and polygons in WGS 84 (SRID 4326), compatible with SQL Server <c>geography</c>.
/// </summary>
public static class Geo
{
    public const int Srid = 4326;

    private static readonly GeometryFactory Factory = NtsGeometryServices.Instance.CreateGeometryFactory(Srid);

    public static Point CreatePoint(double lat, double lng)
    {
        ValidateCoordinate(lat, lng);
        return Factory.CreatePoint(new Coordinate(lng, lat));
    }

    /// <summary>
    /// Creates a closed polygon. The ring is oriented counterclockwise, as <c>geography</c> requires;
    /// stored the other way, SQL Server would read it as "the whole world except the zona".
    /// </summary>
    public static Polygon CreatePolygon(IReadOnlyList<Vertex> vertices)
    {
        if (vertices.Count < 3)
        {
            throw new DomainException("El polígono necesita al menos 3 vértices.");
        }
        foreach (var v in vertices)
        {
            ValidateCoordinate(v.Lat, v.Lng);
        }

        var coordinates = vertices.Select(v => new Coordinate(v.Lng, v.Lat)).ToList();
        if (!coordinates[0].Equals2D(coordinates[^1]))
        {
            coordinates.Add(coordinates[0].Copy());
        }
        if (coordinates.Count < 4)
        {
            throw new DomainException("El polígono necesita al menos 3 vértices distintos.");
        }

        var ring = coordinates.ToArray();
        if (!Orientation.IsCCW(ring))
        {
            Array.Reverse(ring);
        }

        var polygon = Factory.CreatePolygon(ring);
        if (!polygon.IsValid)
        {
            throw new DomainException("El polígono no es válido: sus lados se cruzan.");
        }
        return polygon;
    }

    public static IReadOnlyList<Vertex> ToVertices(Polygon polygon) =>
        polygon.ExteriorRing.Coordinates.Select(c => new Vertex(c.Y, c.X)).ToList();

    private static void ValidateCoordinate(double lat, double lng)
    {
        if (lat is < -90 or > 90 || lng is < -180 or > 180 || double.IsNaN(lat) || double.IsNaN(lng))
        {
            throw new DomainException($"La coordenada ({lat}, {lng}) no es válida.");
        }
    }
}
