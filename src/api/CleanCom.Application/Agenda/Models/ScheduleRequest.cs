using CleanCom.Domain.Pedidos;

namespace CleanCom.Application.Agenda;

/// <summary>
/// Schedule a recolección for a saved invitado and address.
/// </summary>
/// <param name="SpecificTime">Only with <see cref="Turno.HoraEspecifica"/>. Office only.</param>
/// <param name="Priority">Office only.</param>
/// <param name="RegisteredAt">Hora de registro. Empty = now; the office may set an earlier one.</param>
/// <param name="Date">Another visit day chosen from the list. Empty = the first one with cupo.</param>
public record ScheduleRequest(
    int InvitadoId,
    int DireccionId,
    Turno Turno,
    TimeOnly? SpecificTime,
    bool Priority,
    int? GarmentsApprox,
    string? DriverNotes,
    IReadOnlyList<int>? LineaServicioIds,
    DateTimeOffset? RegisteredAt,
    DateOnly? Date);
