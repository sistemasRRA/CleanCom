using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.ParadasFijas;

/// <param name="IsVuelta">More than one paso: the pasos are visited in order and one after the other.</param>
public record ParadaFijaDto(
    int Id,
    string Name,
    ParadaCategory Category,
    ParadaKind Kind,
    int RutaId,
    string RutaCode,
    string RutaColor,
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
    bool IsVuelta,
    IReadOnlyList<ParadaFijaPasoDto> Pasos,
    IReadOnlyList<ParadaFijaExceptionDto> Exceptions);
