namespace CleanCom.Application.Agenda;

/// <summary>
/// Answer of <c>POST /agenda/agendar</c>.
/// </summary>
/// <param name="Scheduled">False when the day filled up meanwhile: nothing was created and
/// <paramref name="Availability"/> carries the next available date.</param>
/// <param name="Message">What happened, in Spanish, to show as is.</param>
public record ScheduleResultDto(
    bool Scheduled,
    int? PedidoId,
    int? ParadaId,
    DateOnly? Date,
    int? Place,
    string Message,
    AvailabilityDto Availability);
