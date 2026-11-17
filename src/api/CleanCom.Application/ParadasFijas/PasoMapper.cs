using CleanCom.Domain.Common;
using CleanCom.Domain.Geography;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using NetTopologySuite.Geometries;

namespace CleanCom.Application.ParadasFijas;

/// <summary>Copies a captured paso into a <see cref="ParadaFijaPaso"/>; shared by paradas fijas and paradas or viajes de un día.</summary>
public static class PasoMapper
{
    /// <param name="number">1, 2, 3… for the error messages.</param>
    /// <param name="kind">The parada's qué se hace: a paso with the same one stores null and follows the parada.</param>
    /// <param name="turno">The parada's turno, same idea.</param>
    public static void Apply(ParadaFijaPaso paso, SaveParadaFijaPasoRequest source, int number, ParadaKind kind, Turno turno)
    {
        paso.Order = number;
        paso.LugarId = source.LugarId;
        paso.Address = source.LugarId is null ? Clean(source.Address) : null;
        paso.Location = source.LugarId is null ? Location(source, number) : null;
        paso.Activity = Clean(source.Activity);
        paso.Kind = source.Kind == kind ? null : source.Kind;
        paso.Turno = source.Turno == turno ? null : source.Turno;
        paso.Before = source.Before;
        paso.Notes = Clean(source.Notes);
    }

    /// <summary>Coordinates as given, or read from the Google Maps link.</summary>
    public static Point? Location(SaveParadaFijaPasoRequest paso, int number)
    {
        if (paso.Lat is { } lat && paso.Lng is { } lng)
        {
            return Geo.CreatePoint(lat, lng);
        }
        if (string.IsNullOrWhiteSpace(paso.MapsLink))
        {
            return null;
        }
        return Geo.TryParseCoordinates(paso.MapsLink, out var a, out var b)
            ? Geo.CreatePoint(a, b)
            : throw new DomainException($"Paso {number}: no encontré coordenadas en el link. Si es un link corto (maps.app.goo.gl), ábrelo y copia el link completo del navegador o las coordenadas (19.36, -99.19).");
    }

    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
