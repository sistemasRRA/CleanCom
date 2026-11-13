using CleanCom.Api.Common;
using CleanCom.Application.ParadasFijas;
using CleanCom.Domain.Access;
using CleanCom.Domain.Routing;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// Paradas y vueltas fijas (docs/pantallas/oficina/paradas-fijas.md). A vuelta fija is a parada fija with several pasos.
/// </summary>
[ApiController]
[Route("paradas-fijas")]
[Tags("Paradas fijas")]
public class ParadasFijasController(ParadaFijaService service) : ControllerBase
{
    /// <summary>All paradas fijas; filters: kind and "ver inactivas".</summary>
    [HttpGet]
    [RequireScreen(Screens.ParadasFijas, AccessLevel.View)]
    public Task<IReadOnlyList<ParadaFijaDto>> List([FromQuery(Name = "tipo")] ParadaKind? kind, [FromQuery(Name = "inactivas")] bool includeInactive, CancellationToken ct) =>
        service.ListAsync(includeInactive, kind, ct);

    /// <summary>"La semana de un vistazo": Monday to Saturday of the week of the date, by ruta, with the cupo they take.</summary>
    [HttpGet("semana")]
    [RequireScreen(Screens.ParadasFijas, AccessLevel.View)]
    public Task<FijasWeekDto> Week([FromQuery(Name = "fecha")] DateOnly? date, CancellationToken ct) =>
        service.WeekAsync(date, ct);

    [HttpGet("{id:int}")]
    [RequireScreen(Screens.ParadasFijas, AccessLevel.View)]
    public Task<ParadaFijaDto> Get(int id, CancellationToken ct) =>
        service.GetAsync(id, ct);

    /// <summary>Creates it and generates its paradas for the days ahead.</summary>
    [HttpPost]
    [RequireScreen(Screens.ParadasFijas, AccessLevel.Edit)]
    public async Task<ActionResult<ParadaFijaDto>> Create(SaveParadaFijaRequest request, CancellationToken ct)
    {
        var fija = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = fija.Id }, fija);
    }

    /// <summary>Saves and replaces its paradas that have not started. Inactive or paused: they leave the rutas.</summary>
    [HttpPut("{id:int}")]
    [RequireScreen(Screens.ParadasFijas, AccessLevel.Edit)]
    public Task<ParadaFijaDto> Update(int id, SaveParadaFijaRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    /// <summary>Skips one date.</summary>
    [HttpPost("{id:int}/excepciones")]
    [RequireScreen(Screens.ParadasFijas, AccessLevel.Edit)]
    public Task<ParadaFijaDto> AddException(int id, SaveParadaFijaExceptionRequest request, CancellationToken ct) =>
        service.AddExceptionAsync(id, request, ct);

    /// <summary>Removes an exception (baja lógica).</summary>
    [HttpDelete("{id:int}/excepciones/{exceptionId:int}")]
    [RequireScreen(Screens.ParadasFijas, AccessLevel.Edit)]
    public Task<ParadaFijaDto> RemoveException(int id, int exceptionId, CancellationToken ct) =>
        service.RemoveExceptionAsync(id, exceptionId, ct);

    /// <summary>"Agregar las fijas del…": replaces the paradas fijas of that day that have not started.</summary>
    [HttpPost("generar")]
    [RequireScreen(Screens.ParadasFijas, AccessLevel.Edit)]
    public Task<GenerateFijasResultDto> Generate([FromQuery(Name = "fecha")] DateOnly date, CancellationToken ct) =>
        service.RegenerateDayAsync(date, ct);
}
