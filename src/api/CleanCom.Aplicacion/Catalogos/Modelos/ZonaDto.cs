using CleanCom.Dominio.Geografia;

namespace CleanCom.Aplicacion.Catalogos;

public record ZonaDto(
    int Id,
    string Codigo,
    int RutaId,
    string RutaCodigo,
    int? ZonaNucleoId,
    string? ZonaNucleoCodigo,
    bool Activo,
    IReadOnlyList<DayOfWeek> DiasVisita,
    IReadOnlyList<DayOfWeek> DiasVisitaEfectivos,
    IReadOnlyList<Vertice>? Poligono);
