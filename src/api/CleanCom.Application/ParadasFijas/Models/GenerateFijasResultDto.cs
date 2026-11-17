namespace CleanCom.Application.ParadasFijas;

/// <param name="Created">Paradas fijas (not pasos) generated.</param>
/// <param name="Replaced">Generated before and replaced because they had not started.</param>
/// <param name="KeptStarted">Left as they were because the chofer had already started them.</param>
public record GenerateFijasResultDto(DateOnly From, DateOnly To, int Created, int Replaced, int KeptStarted, string Message);
