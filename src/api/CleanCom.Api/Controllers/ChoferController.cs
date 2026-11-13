using CleanCom.Api.Common;
using CleanCom.Application.Chofer;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// Endpoints of the chofer app. They work on the operador linked to the signed-in user.
/// </summary>
[ApiController]
[Route("chofer")]
[Tags("App del chofer")]
public class ChoferController(ChoferService service) : ControllerBase
{
    /// <summary>
    /// Paradas of the chofer's ruta on a day (yyyy-MM-dd; today in Mexico City when empty): the ruta the office assigned
    /// that day, or the usual ruta. Provisional order by turno until the ruta is optimized.
    /// </summary>
    [HttpGet("ruta")]
    [RequireScreen(Screens.AppChofer, AccessLevel.View)]
    public Task<ChoferRutaDto> Ruta([FromQuery(Name = "fecha")] DateOnly? date, CancellationToken ct) =>
        service.RutaAsync(date, ct);
}
