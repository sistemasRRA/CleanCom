namespace CleanCom.Application.PostalCodes;

public record PostalCodeStateDto(string State, int Settlements, int PostalCodes);

/// <param name="LastImportAt">When the catalog last changed (UTC); null when it was never imported.</param>
public record PostalCodeSummaryDto(int Settlements, int PostalCodes, IReadOnlyList<PostalCodeStateDto> States, DateTimeOffset? LastImportAt);
