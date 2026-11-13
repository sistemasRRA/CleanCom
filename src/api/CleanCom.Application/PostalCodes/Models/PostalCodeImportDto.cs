namespace CleanCom.Application.PostalCodes;

/// <param name="Read">Settlements in the file.</param>
/// <param name="Deactivated">Settlements of the imported states that are no longer in the official catalog (baja lógica).</param>
/// <param name="States">States the file covered.</param>
public record PostalCodeImportDto(int Read, int Added, int Updated, int Deactivated, IReadOnlyList<string> States);
