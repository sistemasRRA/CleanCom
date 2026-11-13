namespace CleanCom.Application.ParadasFijas;

public record ParadaFijaPasoDto(
    int Order,
    int? LugarId,
    string? LugarName,
    string? Address,
    double? Lat,
    double? Lng,
    string? Activity,
    string? Notes);
