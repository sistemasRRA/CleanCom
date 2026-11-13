using CleanCom.Application.ParadasFijas;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.Rutas;

/// <summary>
/// "Parada de un día" (one paso: convenio, insumos, traspaso…) or "Viaje de un día" (several pasos in order) added to a
/// ruta on one date. Pasos are captured as in paradas fijas.
/// </summary>
/// <param name="Name">"Qué es": "Insumos Florida", "Viaje de bodega a Polanco".</param>
/// <param name="Carried">Insumos o carga / qué se lleva.</param>
public record AddToRutaRequest(
    int RutaId,
    DateOnly Date,
    string Name,
    ParadaCategory Category,
    ParadaKind Kind,
    Turno Turno,
    ParadaPlacement Placement,
    string? Carried,
    string? DriverNotes,
    IReadOnlyList<SaveParadaFijaPasoRequest> Pasos);
