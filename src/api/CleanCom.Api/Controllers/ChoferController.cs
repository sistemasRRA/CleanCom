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
public class ChoferController(ChoferService service, ChoferVisitService visits) : ControllerBase
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

    /// <summary>
    /// Photo of a visit (JPG, PNG or WebP, up to 8 MB), saved on the server's disk. Same localId twice returns the first one.
    /// </summary>
    [HttpPost("evidencias")]
    [RequireScreen(Screens.AppChofer, AccessLevel.Edit)]
    [RequestSizeLimit(ChoferVisitService.MaxPhotoBytes + 64 * 1024)]
    public async Task<EvidenciaDto> UploadPhoto([FromForm] PhotoUpload form, CancellationToken ct)
    {
        await using var content = form.File.OpenReadStream();
        return await visits.UploadPhotoAsync(form.ParadaId, content, form.File.ContentType, form.File.Length, form.LocalId,
            form.Lat, form.Lng, form.TakenAt, ct);
    }

    /// <summary>Motivos for "no realizada".</summary>
    [HttpGet("motivos")]
    [RequireScreen(Screens.AppChofer, AccessLevel.View)]
    public Task<IReadOnlyList<ChoferReasonDto>> Reasons(CancellationToken ct) =>
        visits.ReasonsAsync(ct);

    /// <summary>The invitado's signature as strokes; the API draws it.</summary>
    [HttpPost("evidencias/firma")]
    [RequireScreen(Screens.AppChofer, AccessLevel.Edit)]
    public Task<EvidenciaDto> Sign(SignatureRequest request, CancellationToken ct) =>
        visits.SignAsync(request, ct);

    /// <summary>Cerrar visita: recolección → Recolectado, entrega → Entregado. Photo first when Visita.FotoObligatoria.</summary>
    [HttpPost("paradas/{id:int}/cerrar")]
    [RequireScreen(Screens.AppChofer, AccessLevel.Edit)]
    public Task<ChoferRutaDto> Close(int id, CloseVisitRequest request, CancellationToken ct) =>
        visits.CloseAsync(id, request, ct);

    /// <summary>No realizada: motivo and photo; the pedido is rescheduled to the next day with cupo.</summary>
    [HttpPost("paradas/{id:int}/no-realizada")]
    [RequireScreen(Screens.AppChofer, AccessLevel.Edit)]
    public Task<ChoferRutaDto> NotDone(int id, NotDoneRequest request, CancellationToken ct) =>
        visits.NotDoneAsync(id, request, ct);
}

/// <summary>Multipart form of a photo.</summary>
public class PhotoUpload
{
    public int ParadaId { get; set; }
    public IFormFile File { get; set; } = null!;
    public string? LocalId { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public DateTimeOffset? TakenAt { get; set; }
}
