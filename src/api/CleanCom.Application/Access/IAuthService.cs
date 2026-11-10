namespace CleanCom.Application.Access;

public interface IAuthService
{
    Task<SessionDto> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<SessionDto> RefreshAsync(RefreshSessionRequest request, CancellationToken ct);
    Task LogoutAsync(RefreshSessionRequest request, CancellationToken ct);
    Task<SessionDto> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct);
    Task<SessionUserDto> GetUserAsync(int userId, CancellationToken ct);
}
