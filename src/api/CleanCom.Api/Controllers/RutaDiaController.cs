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

    /// <summary>"Parada de un día": one paso (convenio, insumos, traspaso…) added to a ruta on a date.</summary>
    [HttpPost("paradas")]
    [RequireScreen(Screens.Rutas, AccessLevel.Edit)]
    public Task<RutaPlanDto> AddParada(AddToRutaRequest request, CancellationToken ct) =>
        service.AddAsync(request, false, ct);

    /// <summary>"Viaje de un día": two or more pasos visited in order.</summary>
    [HttpPost("viajes")]
    [RequireScreen(Screens.Rutas, AccessLevel.Edit)]
    public Task<RutaPlanDto> AddViaje(AddToRutaRequest request, CancellationToken ct) =>
        service.AddAsync(request, true, ct);

    /// <summary>Takes a parada (or its whole viaje) out of the ruta that day. Baja lógica; pedidos go through the agenda.</summary>
    [HttpDelete("paradas/{id:int}")]
    [RequireScreen(Screens.Rutas, AccessLevel.Edit)]
    public Task<RutaPlanDto> RemoveParada(int id, CancellationToken ct) =>
        service.RemoveParadaAsync(id, ct);

    /// <summary>Moves a parada and/or pins it; a parada moved by hand stays fixed.</summary>
    [HttpPatch("paradas/{id:int}")]
    [RequireScreen(Screens.Rutas, AccessLevel.Edit)]
    public Task<RutaPlanDto> PatchParada(int id, PatchParadaRequest request, CancellationToken ct) =>
        service.PatchParadaAsync(id, request, ct);
}
