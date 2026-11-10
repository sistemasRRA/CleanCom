using CleanCom.Application.Access;
using CleanCom.Application.Common;
using CleanCom.Domain.Access;
using CleanCom.Domain.Common;
using CleanCom.Domain.Time;
using CleanCom.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Infrastructure.Identity;

public class AuthService(
    UserManager<User> users,
    CleanComDbContext db,
    TokenService tokens,
    IPermissionStore permissionStore,
    IBusinessClock clock,
    IValidator<LoginRequest> loginValidator,
    IValidator<ChangePasswordRequest> changeValidator) : IAuthService
{
    private const string InvalidCredentials = "Usuario o contraseña incorrectos.";
    private const string SessionExpired = "La sesión expiró. Inicia sesión de nuevo.";

    public async Task<SessionDto> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);
        var name = request.UserName.Trim();
        var user = await users.FindByNameAsync(name)
            ?? (name.Contains('@') ? await users.FindByEmailAsync(name) : null);

        if (user is null || !user.Active)
        {
            throw new UnauthorizedException(InvalidCredentials);
        }
        if (await users.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException("La cuenta está bloqueada por varios intentos fallidos. Intenta de nuevo en unos minutos.");
        }
        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            throw new UnauthorizedException(InvalidCredentials);
        }

        await users.ResetAccessFailedCountAsync(user);
        user.LastAccess = clock.UtcNow;
        await users.UpdateAsync(user);
        return await IssueSessionAsync(user, ct);
    }

    public async Task<SessionDto> RefreshAsync(RefreshSessionRequest request, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var hash = TokenService.Hash(request.RefreshToken ?? "");
        var row = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new UnauthorizedException(SessionExpired);

        if (row.RevokedAt is not null)
        {
            // An already used token arrives again: it may have been stolen. Every session of the user is closed.
            await RevokeAllAsync(row.UserId, ct);
            throw new UnauthorizedException(SessionExpired);
        }
        if (row.ExpiresAt <= now)
        {
            throw new UnauthorizedException(SessionExpired);
        }

        row.RevokedAt = now;
        var user = await users.FindByIdAsync(row.UserId.ToString());
        if (user is null || !user.Active)
        {
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedException(SessionExpired);
        }
        return await IssueSessionAsync(user, ct);
    }

    public async Task LogoutAsync(RefreshSessionRequest request, CancellationToken ct)
    {
        var hash = TokenService.Hash(request.RefreshToken ?? "");
        var row = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, ct);
        if (row is not null)
        {
            row.RevokedAt = clock.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<SessionDto> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct)
    {
        await changeValidator.ValidateAndThrowAsync(request, ct);
        var user = await users.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedException(SessionExpired);

        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        IdentityErrors.ThrowIfFailed(result);

        user.MustChangePassword = false;
        await users.UpdateAsync(user);
        await RevokeAllAsync(user.Id, ct);
        return await IssueSessionAsync(user, ct);
    }

    public async Task<SessionUserDto> GetUserAsync(int userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("El usuario no existe.");
        var (role, roleName, permissions) = await RoleAndPermissionsAsync(user, ct);
        return ToSessionUser(user, role, roleName, permissions);
    }

    private async Task<SessionDto> IssueSessionAsync(User user, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var (role, roleName, permissions) = await RoleAndPermissionsAsync(user, ct);
        var (access, expiresAt) = tokens.CreateAccessToken(user, role, now);
        var (refresh, hash, refreshExpiresAt) = tokens.CreateRefreshToken(now);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            CreatedAt = now,
            ExpiresAt = refreshExpiresAt,
        });
        await db.SaveChangesAsync(ct);

        return new SessionDto(access, expiresAt, refresh, refreshExpiresAt, ToSessionUser(user, role, roleName, permissions));
    }

    private async Task<(string Role, string RoleName, IReadOnlyList<string> Permissions)> RoleAndPermissionsAsync(User user, CancellationToken ct)
    {
        var role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? "";
        var roleName = await db.Roles.Where(r => r.Name == role).Select(r => r.DisplayName).FirstOrDefaultAsync(ct) ?? role;
        return (role, roleName, await permissionStore.GetAsync(role, ct));
    }

    private async Task RevokeAllAsync(int userId, CancellationToken ct)
    {
        var now = clock.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    private static SessionUserDto ToSessionUser(User u, string role, string roleName, IReadOnlyList<string> permissions) =>
        new(u.Id, u.FullName, u.UserName ?? "", role, roleName, u.MustChangePassword, permissions);
}

/// <summary>
/// Identity errors in Spanish, since they reach the user.
/// </summary>
public class SpanishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "La contraseña actual no es correcta." };
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"La contraseña debe tener al menos {length} caracteres." };
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = $"El usuario {userName} ya existe." };
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = $"El correo {email} ya está registrado." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = $"El usuario {userName} no es válido." };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = $"El correo {email} no es válido." };
}

internal static class IdentityErrors
{
    public static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new DomainException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
