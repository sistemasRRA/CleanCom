namespace CleanCom.Application.ParadasFijas;

public record ParadaFijaExceptionDto(int Id, DateOnly Date, string Label, string? Reason);
