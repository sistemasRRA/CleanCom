namespace CleanCom.Application.Access;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(string? role, CancellationToken ct);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct);
    Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken ct);

    /// <summary>Roles the current user can assign: never Invitado, and SuperUser only to another SuperUser.</summary>
    Task<IReadOnlyList<RoleOptionDto>> ListAssignableRolesAsync(CancellationToken ct);

    /// <summary>Full names by user id, to show who did something (histories).</summary>
    Task<IReadOnlyDictionary<int, string>> NamesAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
}
