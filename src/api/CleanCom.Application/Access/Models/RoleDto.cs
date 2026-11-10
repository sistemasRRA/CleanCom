using CleanCom.Domain.Access;

namespace CleanCom.Application.Access;

/// <param name="Key">Stored and sent in the token (for example "Logistica").</param>
/// <param name="Name">Shown to the user (for example "Logística").</param>
/// <param name="LockedPermissions">SuperUser: always every office screen, cannot be edited.</param>
/// <param name="AssignableInOffice">False for Invitado, which only signs up from its app.</param>
/// <param name="System">Created with the system: cannot be renamed or deleted.</param>
public record RoleDto(
    int Id,
    string Key,
    string Name,
    bool LockedPermissions,
    bool AssignableInOffice,
    bool System,
    int UserCount,
    IReadOnlyList<RolePermissionDto> Permissions);

public record RolePermissionDto(string Screen, AccessLevel Level);

/// <param name="Group">Side menu section ("Apps" for the mobile apps).</param>
public record ScreenDto(string Key, string Name, string Group);

/// <summary>Role a user can be given from Usuarios (key stored, name shown).</summary>
public record RoleOptionDto(string Key, string Name);
