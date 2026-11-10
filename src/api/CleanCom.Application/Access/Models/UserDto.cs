namespace CleanCom.Application.Access;

public record UserDto(
    int Id,
    string FullName,
    string UserName,
    string? Email,
    string? Phone,
    string Role,
    string RoleName,
    bool Active,
    bool MustChangePassword,
    DateTimeOffset? LastAccess,
    int? OperadorId,
    string? OperadorName);
