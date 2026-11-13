namespace CleanCom.Application.ParadasFijas;

/// <summary>A lugar from the catalog, or an address with optional coordinates.</summary>
public record SaveParadaFijaPasoRequest(int? LugarId, string? Address, double? Lat, double? Lng, string? Activity, string? Notes);
