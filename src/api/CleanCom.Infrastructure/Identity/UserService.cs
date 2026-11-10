using CleanCom.Application.Access;
using CleanCom.Application.Common;
using CleanCom.Domain.Access;
using CleanCom.Domain.Common;
using CleanCom.Domain.People;
using CleanCom.Domain.Time;
using CleanCom.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Infrastructure.Identity;

public class UserService(
    UserManager<User> users,
    CleanComDbContext db,
    IBusinessClock clock,
    ICurrentUser currentUser,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator,
    IValidator<ResetPasswordRequest> resetValidator) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(string? role, CancellationToken ct) =>
        await Query(id: null, role).ToListAsync(ct);

    public async Task<UserDto> CreateAsync(CreateUserRequest r, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(r, ct);
        await EnsureRoleExistsAsync(r.Role, ct);
        await EnsureCanGiveRoleAsync(r.Role, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var user = new User
        {
            UserName = r.UserName.Trim(),
            Email = Clean(r.Email),
            PhoneNumber = Clean(r.Phone),
            FullName = r.FullName.Trim(),
            MustChangePassword = true,
            CreatedAt = clock.UtcNow,
        };
        IdentityErrors.ThrowIfFailed(await users.CreateAsync(user, r.InitialPassword));
        IdentityErrors.ThrowIfFailed(await users.AddToRoleAsync(user, r.Role));

        if (r.OperadorId is { } operadorId)
        {
            await LinkOperadorAsync(user.Id, operadorId, ct);
        }
        else if (r.CreateOperador)
        {
            if (r.UsualRutaId is { } rutaId && !await db.Rutas.AnyAsync(x => x.Id == rutaId, ct))
            {
                throw new NotFoundException($"La ruta {rutaId} no existe.");
            }
            db.Operadores.Add(new Operador
            {
                Name = user.FullName,
                Phone = user.PhoneNumber,
                UsualRutaId = r.UsualRutaId,
                UserId = user.Id,
            });
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await GetAsync(user.Id, ct);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest r, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(r, ct);
        await EnsureRoleExistsAsync(r.Role, ct);
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new NotFoundException($"El usuario {id} no existe.");
        var currentRole = (await users.GetRolesAsync(user)).FirstOrDefault();
        await EnsureCanTouchAsync(currentRole, ct);
        await EnsureCanGiveRoleAsync(r.Role, ct);

        var stopsBeingSuperUser = currentRole == Roles.SuperUser && (r.Role != Roles.SuperUser || !r.Active);
        if (stopsBeingSuperUser && await ActiveSuperUsersAsync(ct) <= 1)
        {
            throw new DomainException("Debe quedar al menos un usuario SuperUser activo.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        user.FullName = r.FullName.Trim();
        user.Email = Clean(r.Email);
        user.PhoneNumber = Clean(r.Phone);
        user.Active = r.Active;
        IdentityErrors.ThrowIfFailed(await users.UpdateAsync(user));

        if (currentRole != r.Role)
        {
            if (currentRole is not null)
            {
                IdentityErrors.ThrowIfFailed(await users.RemoveFromRoleAsync(user, currentRole));
            }
            IdentityErrors.ThrowIfFailed(await users.AddToRoleAsync(user, r.Role));
        }

        var linked = await db.Operadores.FirstOrDefaultAsync(o => o.UserId == id, ct);
        if (linked is not null && linked.Id != r.OperadorId)
        {
            linked.UserId = null;
            await db.SaveChangesAsync(ct);
        }
        if (r.OperadorId is { } operadorId && linked?.Id != operadorId)
        {
            await LinkOperadorAsync(id, operadorId, ct);
        }

        if (!r.Active || currentRole != r.Role)
        {
            await RevokeSessionsAsync(id, ct);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>
    /// The office assigns a temporary password; the user changes it on the next sign-in.
    /// </summary>
    public async Task ResetPasswordAsync(int id, ResetPasswordRequest r, CancellationToken ct)
    {
        await resetValidator.ValidateAndThrowAsync(r, ct);
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new NotFoundException($"El usuario {id} no existe.");
        await EnsureCanTouchAsync((await users.GetRolesAsync(user)).FirstOrDefault(), ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        IdentityErrors.ThrowIfFailed(await users.RemovePasswordAsync(user));
        IdentityErrors.ThrowIfFailed(await users.AddPasswordAsync(user, r.TemporaryPassword));
        user.MustChangePassword = true;
        IdentityErrors.ThrowIfFailed(await users.UpdateAsync(user));
        await users.SetLockoutEndDateAsync(user, null);
        await users.ResetAccessFailedCountAsync(user);
        await RevokeSessionsAsync(id, ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<RoleOptionDto>> ListAssignableRolesAsync(CancellationToken ct)
    {
        var isSuperUser = await CallerIsSuperUserAsync(ct);
        var all = await db.Roles.AsNoTracking().OrderBy(r => r.Id).Select(r => new { r.Name, r.DisplayName }).ToListAsync(ct);
        return
        [
            .. all
                .Where(r => Roles.IsAssignableInOffice(r.Name!) && (isSuperUser || r.Name != Roles.SuperUser))
                .Select(r => new RoleOptionDto(r.Name!, r.DisplayName)),
        ];
    }

    /// <summary>Only a SuperUser makes someone SuperUser; otherwise Administracion could promote itself.</summary>
    private async Task EnsureCanGiveRoleAsync(string role, CancellationToken ct)
    {
        if (role == Roles.SuperUser && !await CallerIsSuperUserAsync(ct))
        {
            throw new DomainException("Solo un usuario SuperUser puede dar el rol SuperUser.");
        }
    }

    /// <summary>Only a SuperUser edits, deactivates or resets the password of another SuperUser.</summary>
    private async Task EnsureCanTouchAsync(string? targetRole, CancellationToken ct)
    {
        if (targetRole == Roles.SuperUser && !await CallerIsSuperUserAsync(ct))
        {
            throw new DomainException("Solo un usuario SuperUser puede modificar a otro SuperUser.");
        }
    }

    private async Task<bool> CallerIsSuperUserAsync(CancellationToken ct) =>
        currentUser.Id is { } id
        && await db.UserRoles.AnyAsync(ur => ur.UserId == id && ur.RoleId == Roles.SuperUserId, ct);

    private async Task EnsureRoleExistsAsync(string role, CancellationToken ct)
    {
        if (!await db.Roles.AnyAsync(x => x.Name == role, ct))
        {
            throw new NotFoundException($"El rol {role} no existe.");
        }
    }

    private async Task LinkOperadorAsync(int userId, int operadorId, CancellationToken ct)
    {
        var operador = await db.Operadores.FindAsync([operadorId], ct) ?? throw new NotFoundException($"El operador {operadorId} no existe.");
        if (operador.UserId is not null && operador.UserId != userId)
        {
            throw new ConflictException($"El operador {operador.Name} ya tiene otro usuario.");
        }
        operador.UserId = userId;
    }

    private Task<int> ActiveSuperUsersAsync(CancellationToken ct) =>
        (from u in db.Users
         join ur in db.UserRoles on u.Id equals ur.UserId
         where ur.RoleId == Roles.SuperUserId && u.Active
         select u.Id).CountAsync(ct);

    private Task RevokeSessionsAsync(int userId, CancellationToken ct)
    {
        var now = clock.UtcNow;
        return db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    private async Task<UserDto> GetAsync(int id, CancellationToken ct) =>
        await Query(id, role: null).FirstAsync(ct);

    /// <summary>Filters go before the projection to the DTO so EF can translate them to SQL.</summary>
    private IQueryable<UserDto> Query(int? id, string? role) =>
        from u in db.Users.AsNoTracking()
        join ur in db.UserRoles on u.Id equals ur.UserId
        join r in db.Roles on ur.RoleId equals r.Id
        join o in db.Operadores on (int?)u.Id equals o.UserId into operadores
        from o in operadores.DefaultIfEmpty()
        where (id == null || u.Id == id) && (role == null || r.Name == role)
        orderby u.FullName
        select new UserDto(
            u.Id,
            u.FullName,
            u.UserName!,
            u.Email,
            u.PhoneNumber,
            r.Name!,
            r.DisplayName,
            u.Active,
            u.MustChangePassword,
            u.LastAccess,
            o == null ? null : o.Id,
            o == null ? null : o.Name);

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
