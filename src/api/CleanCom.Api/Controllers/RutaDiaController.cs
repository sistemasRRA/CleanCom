using CleanCom.Api.Common;
using CleanCom.Application.Rutas;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// Planning of the rutas of a day (docs/pantallas/oficina/rutas.md).
/// </summary>
[ApiController]
[Tags("Rutas del día")]
public class RutaDiaController(RutaDiaService service) : ControllerBase
{
    /// <summary>Every active ruta on a day (today when empty): chofer, paradas in order with estimated times, km, return and alerts.</summary>
    [HttpGet("rutas/dia")]
    [RequireScreen(Screens.Rutas, AccessLevel.View)]
    public Task<RutasDiaDto> Day([FromQuery(Name = "fecha")] DateOnly? date, CancellationToken ct) =>
        service.DayAsync(date, ct);

    /// <summary>The chofer of a ruta on a day; null goes back to the operador whose usual ruta it is.</summary>
    [HttpPut("rutas/{rutaId:int}/dias/{fecha}/chofer")]
    [RequireScreen(Screens.Rutas, AccessLevel.Edit)]
    public Task<RutaPlanDto> AssignOperador(int rutaId, [FromRoute(Name = "fecha")] DateOnly date, AssignOperadorRequest request, CancellationToken ct) =>
        service.AssignOperadorAsync(rutaId, date, request, ct);

    /// <summary>Orders the ruta. Started paradas stay first, pinned ones keep their place, viajes stay together.</summary>
    [HttpPost("rutas/dia/{id:int}/optimizar")]
    [RequireScreen(Screens.Rutas, AccessLevel.Edit)]
    public Task<RutaPlanDto> Optimize(int id, CancellationToken ct) =>
        service.OptimizeAsync(id, ct);

    /// <summary>Moves a parada (with its viaje) and/or pins it; a parada moved by hand stays fixed.</summary>
    [HttpPatch("paradas/{id:int}")]
    [RequireScreen(Screens.Rutas, AccessLevel.Edit)]
    public Task<RutaPlanDto> PatchParada(int id, PatchParadaRequest request, CancellationToken ct) =>
        service.PatchParadaAsync(id, request, ct);
}
