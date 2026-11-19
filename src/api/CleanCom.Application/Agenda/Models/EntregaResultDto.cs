namespace CleanCom.Application.Agenda;

/// <summary>Programar entrega, cambiar fecha or the rescheduling after a failed attempt.</summary>
/// <param name="Scheduled">False when the chosen day ran out of cupo; <paramref name="Message"/> proposes the next one.</param>
public record EntregaResultDto(bool Scheduled, DateOnly? Date, int? Place, string Message, AvailabilityDto Availability);
