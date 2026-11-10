namespace CleanCom.Application.Access;

public record UpdateUserRequest(string FullName, string? Email, string? Phone, string Role, bool Active, int? OperadorId);
