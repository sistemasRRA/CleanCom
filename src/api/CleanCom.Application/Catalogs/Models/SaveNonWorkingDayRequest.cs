namespace CleanCom.Application.Catalogs;

public record SaveNonWorkingDayRequest(DateOnly Date, string Reason);
