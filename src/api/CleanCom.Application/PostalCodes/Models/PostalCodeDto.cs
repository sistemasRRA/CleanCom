namespace CleanCom.Application.PostalCodes;

/// <summary>A colonia of the official catalog: what the address form fills in from the postal code.</summary>
public record PostalCodeDto(string Code, string Settlement, string SettlementType, string Borough, string? City, string State);
