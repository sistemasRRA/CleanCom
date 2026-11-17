namespace CleanCom.Application.ParadasFijas;

public record FijasWeekDayDto(DateOnly Date, string Label, string? NonWorkingReason, IReadOnlyList<FijasWeekCellDto> Rutas);
