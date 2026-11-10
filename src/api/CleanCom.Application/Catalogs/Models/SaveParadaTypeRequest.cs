namespace CleanCom.Application.Catalogs;

public record SaveParadaTypeRequest(string Name, string? RequiredFields, bool Active = true);
