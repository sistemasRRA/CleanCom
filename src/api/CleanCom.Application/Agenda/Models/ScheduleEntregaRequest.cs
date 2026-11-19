namespace CleanCom.Application.Agenda;

/// <param name="Date">Chosen day; empty = the first day with cupo of its zona.</param>
public record ScheduleEntregaRequest(DateOnly? Date);
