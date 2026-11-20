using CleanCom.Api.Common;
using CleanCom.Application.Chofer;
using CleanCom.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

[ApiController]
[Route("evidencias")]
[Tags("Evidencias")]
public class EvidenceController(EvidenceService service) : ControllerBase
{
    /// <summary>The photo or signature of a visit.</summary>
    [HttpGet("{id:int}/archivo")]
    [RequireScreen(Screens.Pedidos, AccessLevel.View)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> File(int id, CancellationToken ct)
    {
        var (content, type) = await service.OpenAsync(id, ct);
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'";
        Response.Headers.CacheControl = "private, max-age=86400";
        return File(content, type);
    }
}
