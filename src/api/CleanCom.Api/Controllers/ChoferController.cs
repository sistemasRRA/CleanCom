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
    /// that day, or the usual ruta. Same order and estimated times as the Rutas screen.
    /// </summary>
    [HttpGet("ruta")]
    [RequireScreen(Screens.AppChofer, AccessLevel.View)]
    public Task<ChoferRutaDto> Ruta([FromQuery(Name = "fecha")] DateOnly? date, CancellationToken ct) =>
        service.RutaAsync(date, ct);

    /// <summary>"En camino" to a parada of today's ruta: in the assigned order and one at a time.</summary>
    [HttpPost("paradas/{id:int}/en-camino")]
    [RequireScreen(Screens.AppChofer, AccessLevel.Edit)]
    public Task<ChoferRutaDto> EnCamino(int id, ChoferLocationRequest request, CancellationToken ct) =>
        service.EnCaminoAsync(id, request, ct);

    /// <summary>
    /// "Llegué": real arrival time. Farther than Gps.ToleranciaMetros answers 409 until the chofer confirms
    /// (confirmOutsideRadius); the office sees it marked.
    /// </summary>
    [HttpPost("paradas/{id:int}/llegue")]
    [RequireScreen(Screens.AppChofer, AccessLevel.Edit)]
    public Task<ChoferRutaDto> Arrive(int id, ArriveRequest request, CancellationToken ct) =>
        service.ArriveAsync(id, request, ct);
}
