using CleanCom.Api.Common;
using CleanCom.Application.Access;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Roles and the screens each one can see or edit (Roles y permisos).</summary>
[Tags("Roles y permisos")]
[ApiController]
[Route("roles")]
public class RolesController(IRoleService service) : ControllerBase
{
    /// <summary>Every role with its permissions and how many users it has.</summary>
    [HttpGet]
    [RequireScreen(Screens.RolesYPermisos, AccessLevel.View)]
    public Task<IReadOnlyList<RoleDto>> List(CancellationToken ct) => service.ListAsync(ct);

    /// <summary>Screens a permission can be granted on, with their side menu group.</summary>
    [HttpGet("pantallas")]
    [RequireScreen(Screens.RolesYPermisos, AccessLevel.View)]
    public IReadOnlyList<ScreenDto> ListScreens() =>
        [.. Screens.All.Select(s => new ScreenDto(s.Key, s.Name, s.Group))];

    [HttpPost]
    [RequireScreen(Screens.RolesYPermisos, AccessLevel.Edit)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveRoleRequest request, CancellationToken ct)
    {
        var role = await service.CreateAsync(request, ct);
        return Created($"/roles/{role.Id}", role);
    }

    /// <summary>Changes the visible name of a role created by the office.</summary>
    [HttpPut("{id:int}")]
    [RequireScreen(Screens.RolesYPermisos, AccessLevel.Edit)]
    public Task<RoleDto> Rename(int id, SaveRoleRequest request, CancellationToken ct) =>
        service.RenameAsync(id, request, ct);

    /// <summary>Replaces the permissions of the role. Applies to its users on their next request.</summary>
    [HttpPut("{id:int}/permisos")]
    [RequireScreen(Screens.RolesYPermisos, AccessLevel.Edit)]
    public Task<RoleDto> SavePermissions(int id, SaveRolePermissionsRequest request, CancellationToken ct) =>
        service.SavePermissionsAsync(id, request, ct);

    /// <summary>Only roles created by the office and without users.</summary>
    [HttpDelete("{id:int}")]
    [RequireScreen(Screens.RolesYPermisos, AccessLevel.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
