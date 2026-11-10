using CleanCom.Api.Common;
using CleanCom.Application.Catalogs;
using CleanCom.Domain.Access;
using CleanCom.Domain.Catalogs;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("motivos")]
[Tags("Catálogos")]
public class ReasonsController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ReasonDto>> List([FromQuery] ReasonType? type, CancellationToken ct) =>
        service.ListReasonsAsync(type, ct);

    [HttpPost]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType<ReasonDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveReasonRequest request, CancellationToken ct)
    {
        var reason = await service.SaveReasonAsync(null, request, ct);
        return Created($"/motivos/{reason.Id}", reason);
    }

    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    public Task<ReasonDto> Update(int id, SaveReasonRequest request, CancellationToken ct) =>
        service.SaveReasonAsync(id, request, ct);
}
