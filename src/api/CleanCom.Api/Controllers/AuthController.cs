using CleanCom.Api.Common;
using CleanCom.Application.Access;
using CleanCom.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Sign in and out, token refresh and current user data.</summary>
[ApiController]
[Route("auth")]
[Tags("Sesión")]
public class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<SessionDto> Login(LoginRequest request, CancellationToken ct) =>
        service.LoginAsync(request, ct);

    [HttpPost("renovar")]
    [AllowAnonymous]
    public Task<SessionDto> Refresh(RefreshSessionRequest request, CancellationToken ct) =>
        service.RefreshAsync(request, ct);

    [HttpPost("cerrar")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshSessionRequest request, CancellationToken ct)
    {
        await service.LogoutAsync(request, ct);
        return NoContent();
    }

    /// <summary>Available even while the initial password has not been changed.</summary>
    [HttpPost("cambiar-contrasena")]
    [Authorize(Policies.SignedIn)]
    public Task<SessionDto> ChangePassword(ChangePasswordRequest request, CancellationToken ct) =>
        service.ChangePasswordAsync(UserId(), request, ct);

    /// <summary>Available even while the initial password has not been changed.</summary>
    [HttpGet("yo")]
    [Authorize(Policies.SignedIn)]
    public Task<SessionUserDto> Me(CancellationToken ct) =>
        service.GetUserAsync(UserId(), ct);

    private int UserId() =>
        User.GetUserId() ?? throw new UnauthorizedException("La sesión no es válida.");
}
