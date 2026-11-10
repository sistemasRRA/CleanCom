using CleanCom.Api.Common;
using CleanCom.Application.Catalogs;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Postal codes and neighborhoods served, linked to their zona.</summary>
[ApiController]
[Route("cobertura")]
[Tags("Catálogos")]
public class CoverageController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CoverageDto>> List([FromQuery] string? search, [FromQuery] int? zonaId, CancellationToken ct) =>
        service.ListCoverageAsync(search, zonaId, ct);

    [HttpPost]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType<CoverageDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveCoverageRequest request, CancellationToken ct)
    {
        var coverage = await service.SaveCoverageAsync(null, request, ct);
        return Created($"/cobertura/{coverage.Id}", coverage);
    }

    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    public Task<CoverageDto> Update(int id, SaveCoverageRequest request, CancellationToken ct) =>
        service.SaveCoverageAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteCoverageAsync(id, ct);
        return NoContent();
    }
}
