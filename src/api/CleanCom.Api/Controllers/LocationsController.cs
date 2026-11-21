using CleanCom.Api.Common;
using CleanCom.Application.Common;
using CleanCom.Application.Tracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// Where the choferes are, for the live map (Panel del día and Rutas). Changes arrive through the hub "operacion".
/// </summary>
[ApiController]
[Tags("Ubicaciones")]
[Authorize(Policy = Policies.SeesOperacion)]
public class LocationsController(LocationService service) : ControllerBase
{
    /// <summary>Last known position of each ruta on a day (yyyy-MM-dd; today in Mexico City when empty).</summary>
    [HttpGet("ubicaciones")]
    public Task<IReadOnlyList<LiveLocationDto>> Live([FromQuery(Name = "fecha")] DateOnly? date, CancellationToken ct) =>
        service.LiveAsync(date, ct);

    /// <summary>Where the chofer went on a ruta del día, in order, to compare with the planned ruta.</summary>
    [HttpGet("rutas/dia/{id:int}/recorrido")]
    public Task<TrackDto> Track(int id, CancellationToken ct) =>
        service.TrackAsync(id, ct);
}
