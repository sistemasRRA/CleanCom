using CleanCom.Api.Common;
using CleanCom.Application.Access;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("usuarios")]
[Tags("Usuarios")]
public class UsersController(IUserService service) : ControllerBase
{
    [HttpGet]
    [RequireScreen(Screens.Usuarios, AccessLevel.View)]
    public Task<IReadOnlyList<UserDto>> List([FromQuery] string? role, CancellationToken ct) =>
        service.ListAsync(role, ct);

    [HttpPost]
    [RequireScreen(Screens.Usuarios, AccessLevel.Edit)]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await service.CreateAsync(request, ct);
        return Created($"/usuarios/{user.Id}", user);
    }

    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Usuarios, AccessLevel.Edit)]
    public Task<UserDto> Update(int id, UpdateUserRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    /// <summary>Assigns a temporary password; the user must change it on the next sign-in.</summary>
    [HttpPost("{id:int}/restablecer")]
    [RequireScreen(Screens.Usuarios, AccessLevel.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken ct)
    {
        await service.ResetPasswordAsync(id, request, ct);
        return NoContent();
    }
}
