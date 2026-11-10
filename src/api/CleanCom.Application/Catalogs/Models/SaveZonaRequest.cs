using CleanCom.Domain.Geography;

namespace CleanCom.Application.Catalogs;

/// <param name="Polygon">Vertices as latitude and longitude; it is closed automatically. Empty = no polygon yet.</param>
public record SaveZonaRequest(string Code, int RutaId, int? ZonaNucleoId, IReadOnlyList<Vertex>? Polygon, bool Active = true);
