namespace CleanCom.Application.Access;

/// <param name="Name">Name shown to the user; the key is derived from it when the role is created.</param>
public record SaveRoleRequest(string Name);

/// <summary>Replaces every permission of the role. A screen left out means no access.</summary>
public record SaveRolePermissionsRequest(IReadOnlyList<RolePermissionDto> Permissions);
