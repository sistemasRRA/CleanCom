using CleanCom.Api.Common;
using CleanCom.Application.Promociones;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Sellos and cuponera with their numbers (docs/pantallas/oficina/promociones.md). Campaigns arrive with notifications.</summary>
[ApiController]
[Route("promociones")]
[Tags("Promociones")]
public class PromocionesController(PromocionService service) : ControllerBase
{
    /// <summary>Indicators per program (granted, used, redemption against its goal) and every promotion with its numbers.</summary>
    [HttpGet]
    [RequireScreen(Screens.Promociones, AccessLevel.View)]
    public Task<PromocionesBoardDto> Board(CancellationToken ct) => service.BoardAsync(ct);

    [HttpPost]
    [RequireScreen(Screens.Promociones, AccessLevel.Edit)]
    [ProducesResponseType<PromocionDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SavePromocionRequest request, CancellationToken ct)
    {
        var promocion = await service.SaveAsync(null, request, ct);
        return Created($"/promociones/{promocion.Id}", promocion);
    }

    /// <summary>Changes apply to beneficios granted from now on; inactive ones are no longer granted.</summary>
    [HttpPut("{id:int}")]
    [RequireScreen(Screens.Promociones, AccessLevel.Edit)]
    public Task<PromocionDto> Update(int id, SavePromocionRequest request, CancellationToken ct) => service.SaveAsync(id, request, ct);
}

/// <summary>Beneficios by their unique code.</summary>
[ApiController]
[Route("beneficios")]
[Tags("Promociones")]
public class BeneficiosController(PromocionService service) : ControllerBase
{
    /// <summary>Marks a beneficio as used. A used or expired one answers 400.</summary>
    [HttpPost("{codigo}/usar")]
    [Authorize(Policy = Policies.UsesBeneficios)]
    public Task<BeneficioDto> Use(string codigo, CancellationToken ct) => service.UseAsync(codigo, ct);
}
