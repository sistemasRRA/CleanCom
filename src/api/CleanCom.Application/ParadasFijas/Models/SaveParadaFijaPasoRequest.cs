namespace CleanCom.Application.ParadasFijas;

/// <summary>A lugar from the catalog, or an address with its location.</summary>
/// <param name="MapsLink">"Link de Google Maps o coordenadas": a full Google Maps link or "19.36,-99.19". Used when Lat and Lng are empty.</param>
/// <param name="Activity">Indication for the chofer at this paso ("Cargar en Leones").</param>
public record SaveParadaFijaPasoRequest(int? LugarId, string? Address, double? Lat, double? Lng, string? MapsLink, string? Activity, string? Notes);
