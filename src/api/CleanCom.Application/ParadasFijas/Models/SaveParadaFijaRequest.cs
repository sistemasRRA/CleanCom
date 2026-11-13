using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.ParadasFijas;

/// <param name="Pasos">In visiting order. One paso is a parada fija; several are a vuelta fija.</param>
/// <param name="Paused">Stays configured but generates nothing until resumed.</param>
public record SaveParadaFijaRequest(
    string Name,
    ParadaKind Kind,
    int RutaId,
    Turno Turno,
    ParadaPlacement Placement,
    IReadOnlyList<DayOfWeek> Days,
    string? Carried,
    string? SiteContact,
    string? SitePhone,
    int? GarmentsApprox,
    string? DriverNotes,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil,
    bool Paused,
    bool Active,
    IReadOnlyList<SaveParadaFijaPasoRequest> Pasos);
