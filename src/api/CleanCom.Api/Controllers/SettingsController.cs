using CleanCom.Api.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>Configurable values of the Operación screen: cutoffs, cupos, goals, turnos, thresholds.</summary>
[ApiController]
[Route("configuracion")]
[Tags("Configuración")]
public class SettingsController(ISettingService service) : ControllerBase
{
    [HttpGet]
    [RequireScreen(Screens.Operacion, AccessLevel.View)]
    public Task<IReadOnlyList<SettingDto>> List(CancellationToken ct) =>
        service.ListAsync(ct);

    /// <summary>Changes a value from now on or from a future date.</summary>
    [HttpPut]
    [RequireScreen(Screens.Operacion, AccessLevel.Edit)]
    public Task<SettingHistoryDto> Change(ChangeSettingRequest request, CancellationToken ct) =>
        service.ChangeAsync(request, ct);

    [HttpGet("historial")]
    [RequireScreen(Screens.Operacion, AccessLevel.View)]
    public Task<IReadOnlyList<SettingHistoryDto>> History([FromQuery] string? key, CancellationToken ct) =>
        service.HistoryAsync(key, ct);
}
