namespace CleanCom.Application.Catalogs;

public record SaveLineaServicioRequest(string Name, int? ProcessingDays, bool Active = true);
