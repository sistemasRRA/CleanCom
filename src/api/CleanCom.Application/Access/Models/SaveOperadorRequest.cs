namespace CleanCom.Application.Access;

public record SaveOperadorRequest(string Name, string? Phone, int? UsualRutaId, bool Active = true);
