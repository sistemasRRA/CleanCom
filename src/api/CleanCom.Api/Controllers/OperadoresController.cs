using CleanCom.Api.Common;
using CleanCom.Application.Access;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("operadores")]
[Tags("Usuarios")]
public class OperadoresController(OperadorService service) : ControllerBase
{
    [HttpGet]
    [RequireScreen(Screens.Usuarios, AccessLevel.View)]
    public Task<IReadOnlyList<OperadorDto>> List([FromQuery] bool? includeInactive, CancellationToken ct) =>
        service.ListAsync(includeInactive ?? false, ct);

    [HttpPost]
    [RequireScreen(Screens.Usuarios, AccessLevel.Edit)]
    [ProducesResponseType<OperadorDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveOperadorRequest request, CancellationToken ct)
    {
        var operador = await service.SaveAsync(null, request, ct);
        return Created($"/operadores/{operador.Id}", operador);
    }

    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Usuarios, AccessLevel.Edit)]
    public Task<OperadorDto> Update(int id, SaveOperadorRequest request, CancellationToken ct) =>
        service.SaveAsync(id, request, ct);
}
