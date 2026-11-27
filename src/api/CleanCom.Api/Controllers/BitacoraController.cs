using CleanCom.Api.Common;
using CleanCom.Application.ChangeLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// The general bitácora: who changed what in invitados, direcciones, pedidos, rutas, zonas, metas de KPI, usuarios and
/// permisos. SuperUser and Administración only; each screen shows the bitácora of its own records.
/// </summary>
[ApiController]
[Route("bitacora")]
[Tags("Bitácora")]
[Authorize(Policies.Administrators)]
public class BitacoraController(ChangeLogService service) : ControllerBase
{
    /// <summary>
    /// Newest first. <c>entidad</c> (and <c>id</c>) also bring what belongs to that record (a ruta with its zonas, a
    /// user with their role, an invitado with their addresses); <c>hasta</c> is included.
    /// </summary>
    [HttpGet]
    public Task<ChangeLogPageDto> Search(
        CancellationToken ct,
        [FromQuery(Name = "entidad")] string? entity = null,
        [FromQuery(Name = "id")] int? entityId = null,
        [FromQuery(Name = "usuario")] int? userId = null,
        [FromQuery(Name = "desde")] DateOnly? from = null,
        [FromQuery(Name = "hasta")] DateOnly? to = null,
        [FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "porPagina")] int pageSize = ChangeLogService.DefaultPageSize) =>
        service.SearchAsync(new ChangeLogQuery(entity, entityId, userId, from, to), page, pageSize, ct);
}
