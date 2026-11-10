namespace CleanCom.Application.Access;

/// <param name="Role">Role key, for example "Logistica".</param>
/// <param name="RoleName">Role name shown to the user, for example "Logística".</param>
/// <param name="Permissions">"screen:level" strings, for example "rutas:edit".</param>
public record SessionUserDto(
    int Id,
    string FullName,
    string UserName,
    string Role,
    string RoleName,
    bool MustChangePassword,
    IReadOnlyList<string> Permissions);
