namespace CleanCom.Application.Access;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct);
    Task<RoleDto> CreateAsync(SaveRoleRequest request, CancellationToken ct);
    Task<RoleDto> RenameAsync(int id, SaveRoleRequest request, CancellationToken ct);
    Task<RoleDto> SavePermissionsAsync(int id, SaveRolePermissionsRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

/// <summary>
/// Permissions per role read from the database, cached in memory. Every request checks them here,
/// so a change made in Roles y permisos applies right away without signing in again.
/// </summary>
public interface IPermissionStore
{
    /// <summary>"screen:level" strings of the role; empty if the role does not exist.</summary>
    Task<IReadOnlyList<string>> GetAsync(string role, CancellationToken ct);

    void Invalidate();
}
