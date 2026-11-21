using CleanCom.Api.Common;
using CleanCom.Application.Panel;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Tags("Panel del día")]
public class PanelController(PanelService service) : ControllerBase
{
    /// <summary>
    /// The day's operation (yyyy-MM-dd; today in Mexico City when empty): indicators, pending tray, a card per ruta,
    /// the next 7 days' load and the latest visits. Changes arrive through the hub "operacion".
    /// </summary>
    [HttpGet("panel")]
    [RequireScreen(Screens.PanelDelDia, AccessLevel.View)]
    public Task<PanelDto> Get([FromQuery(Name = "fecha")] DateOnly? date, CancellationToken ct) =>
        service.GetAsync(date, ct);
}
