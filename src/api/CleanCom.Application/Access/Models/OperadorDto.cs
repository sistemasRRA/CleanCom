namespace CleanCom.Application.Access;

public record OperadorDto(int Id, string Name, string? Phone, int? UsualRutaId, bool Active, int? UserId);
