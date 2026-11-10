using CleanCom.Application.Access;
using CleanCom.Application.Common;
using CleanCom.Domain.Access;
using CleanCom.Domain.Common;
using CleanCom.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Infrastructure.Identity;

/// <summary>
/// Roles and the screens each one sees (Usuarios › Roles y permisos).
/// </summary>
public class RoleService(
    RoleManager<Role> roles,
    CleanComDbContext db,
    IPermissionStore store,
    IValidator<SaveRoleRequest> roleValidator,
    IValidator<SaveRolePermissionsRequest> permissionsValidator) : IRoleService
{
    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct)
    {
        var all = await db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);
        var counts = await db.UserRoles
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, ct);
        var permissions = await db.RolePermissions.AsNoTracking().ToListAsync(ct);

        return [.. all.Select(r => ToDto(r, counts.GetValueOrDefault(r.Id), permissions.Where(p => p.RoleId == r.Id)))];
    }

    public async Task<RoleDto> CreateAsync(SaveRoleRequest r, CancellationToken ct)
    {
        await roleValidator.ValidateAndThrowAsync(r, ct);
        var name = r.Name.Trim();
        var key = Roles.KeyFromName(name);
        if (await roles.RoleExistsAsync(key) || await db.Roles.AnyAsync(x => x.DisplayName == name, ct))
        {
            throw new ConflictException($"Ya existe el rol {name}.");
        }

        var role = new Role(key) { DisplayName = name };
        IdentityErrors.ThrowIfFailed(await roles.CreateAsync(role));
        return await GetAsync(role.Id, ct);
    }

    public async Task<RoleDto> RenameAsync(int id, SaveRoleRequest r, CancellationToken ct)
    {
        await roleValidator.ValidateAndThrowAsync(r, ct);
        var role = await FindAsync(id, ct);
        if (Roles.IsSystem(role.Name!))
        {
            throw new DomainException($"El rol {role.DisplayName} es del sistema y no se renombra.");
        }

        var name = r.Name.Trim();
        if (await db.Roles.AnyAsync(x => x.Id != id && x.DisplayName == name, ct))
        {
            throw new ConflictException($"Ya existe el rol {name}.");
        }

        // Only the visible name changes; the key stays so tokens and stored data keep working.
        role.DisplayName = name;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<RoleDto> SavePermissionsAsync(int id, SaveRolePermissionsRequest r, CancellationToken ct)
    {
        await permissionsValidator.ValidateAndThrowAsync(r, ct);
        var role = await FindAsync(id, ct);
        if (Roles.HasLockedPermissions(role.Name!))
        {
            throw new DomainException("SuperUser siempre ve y edita todas las pantallas de oficina; sus permisos no se cambian.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.RolePermissions.Where(p => p.RoleId == id).ExecuteDeleteAsync(ct);
        db.RolePermissions.AddRange(r.Permissions.Select(p => new RolePermission
        {
            RoleId = id,
            Screen = p.Screen,
            Level = p.Level,
        }));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        store.Invalidate();
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var role = await FindAsync(id, ct);
        if (Roles.IsSystem(role.Name!))
        {
            throw new DomainException($"El rol {role.DisplayName} es del sistema y no se borra.");
        }
        var users = await db.UserRoles.CountAsync(ur => ur.RoleId == id, ct);
        if (users > 0)
        {
            throw new ConflictException($"El rol {role.DisplayName} tiene {users} usuario(s). Cámbialos de rol antes de borrarlo.");
        }

        // Its permissions are deleted in cascade.
        IdentityErrors.ThrowIfFailed(await roles.DeleteAsync(role));
        store.Invalidate();
    }

    private async Task<Role> FindAsync(int id, CancellationToken ct) =>
        await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new NotFoundException($"El rol {id} no existe.");

    private async Task<RoleDto> GetAsync(int id, CancellationToken ct) =>
        (await ListAsync(ct)).First(r => r.Id == id);

    private static RoleDto ToDto(Role r, int userCount, IEnumerable<RolePermission> permissions)
    {
        var key = r.Name!;
        IReadOnlyList<RolePermissionDto> list = Roles.HasLockedPermissions(key)
            ? [.. Permissions.FullOffice.Select(p => new RolePermissionDto(p.Screen, p.Level))]
            : [.. permissions.OrderBy(p => p.Screen).Select(p => new RolePermissionDto(p.Screen, p.Level))];
        return new RoleDto(
            r.Id,
            key,
            r.DisplayName,
            Roles.HasLockedPermissions(key),
            Roles.IsAssignableInOffice(key),
            Roles.IsSystem(key),
            userCount,
            list);
    }
}
