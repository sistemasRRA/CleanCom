using CleanCom.Api.Common;
using CleanCom.Application.Catalogs;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("dias-inhabiles")]
[Tags("Catálogos")]
public class NonWorkingDaysController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<NonWorkingDayDto>> List([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
        service.ListNonWorkingDaysAsync(from, to, ct);

    [HttpPost]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType<NonWorkingDayDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveNonWorkingDayRequest request, CancellationToken ct)
    {
        var day = await service.SaveNonWorkingDayAsync(null, request, ct);
        return Created($"/dias-inhabiles/{day.Id}", day);
    }

    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    public Task<NonWorkingDayDto> Update(int id, SaveNonWorkingDayRequest request, CancellationToken ct) =>
        service.SaveNonWorkingDayAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteNonWorkingDayAsync(id, ct);
        return NoContent();
    }
}
