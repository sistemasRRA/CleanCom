namespace CleanCom.Application.Catalogs;

public record SaveCoverageRequest(string PostalCode, string Neighborhood, string Borough, int ZonaId);
