namespace CleanCom.Application.Catalogs;

public record RutaDto(int Id, string Code, string Name, string Color, int? CupoParadas, int? RecoleccionGoal, TimeOnly? MaxReturnTime, TimeOnly? MaxReturnTimeSaturday, bool Active);
