using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.ParadasFijas;

/// <summary>A lugar from the catalog, or an address with its location.</summary>
/// <param name="MapsLink">"Link de Google Maps o coordenadas": a full Google Maps link or "19.36,-99.19". Used when Lat and Lng are empty.</param>
/// <param name="Kind">Qué se hace in this paso; null = the parada fija's.</param>
/// <param name="Turno">Turno of this paso; null = the parada fija's.</param>
/// <param name="Before">"Antes: a las 9:30 a.m.": latest time to be there.</param>
/// <param name="Activity">Indication for the chofer at this paso ("Cargar en Leones").</param>
public record SaveParadaFijaPasoRequest(
    int? LugarId,
    string? Address,
    double? Lat,
    double? Lng,
    string? MapsLink,
    ParadaKind? Kind,
    Turno? Turno,
    TimeOnly? Before,
    string? Activity,
    string? Notes);
