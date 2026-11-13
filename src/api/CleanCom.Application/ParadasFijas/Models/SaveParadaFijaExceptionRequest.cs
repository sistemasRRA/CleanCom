namespace CleanCom.Application.ParadasFijas;

/// <summary>A date the parada fija is skipped.</summary>
public record SaveParadaFijaExceptionRequest(DateOnly Date, string? Reason);
