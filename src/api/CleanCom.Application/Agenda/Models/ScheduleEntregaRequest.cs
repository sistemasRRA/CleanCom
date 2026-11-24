using CleanCom.Domain.Pedidos;

namespace CleanCom.Application.Agenda;

/// <param name="Date">Chosen day; empty = the first day with cupo of its zona.</param>
/// <param name="Turno">Mañana, tarde or cualquier hora (no specific time); empty = the turno of its last entrega, or the
/// recolección's.</param>
public record ScheduleEntregaRequest(DateOnly? Date, Turno? Turno = null);
