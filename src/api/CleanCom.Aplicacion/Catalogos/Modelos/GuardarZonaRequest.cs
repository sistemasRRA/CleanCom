using CleanCom.Dominio.Geografia;

namespace CleanCom.Aplicacion.Catalogos;

/// <param name="Poligono">Vértices en latitud y longitud; se cierra solo. Vacío = sin polígono todavía.</param>
public record GuardarZonaRequest(string Codigo, int RutaId, int? ZonaNucleoId, IReadOnlyList<Vertice>? Poligono, bool Activo = true);
