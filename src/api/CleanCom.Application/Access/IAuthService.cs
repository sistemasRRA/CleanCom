namespace CleanCom.Application.Access;

public interface IAuthService
{
    Task<SessionDto> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<SessionDto> RefreshAsync(RefreshSessionRequest request, CancellationToken ct);
    Task LogoutAsync(RefreshSessionRequest request, CancellationToken ct);
    Task<SessionDto> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct);
    Task<SessionUserDto> GetUserAsync(int userId, CancellationToken ct);

    /// <summary>Invitado app: sends a sign-in code to the phone (WhatsApp; SMS as backup).</summary>
    Task<CodeSentDto> SendCodeAsync(SendCodeRequest request, CancellationToken ct);

    /// <summary>
    /// Invitado app: checks the code and opens a session. The phone links to its invitado in the CRM, or a new one is
    /// created; the invitado gets a user with the Invitado role the first time.
    /// </summary>
    Task<InvitadoSessionDto> VerifyCodeAsync(VerifyCodeRequest request, CancellationToken ct);
}
