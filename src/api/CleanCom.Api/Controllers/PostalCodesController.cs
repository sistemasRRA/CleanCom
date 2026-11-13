using CleanCom.Api.Common;
using CleanCom.Application.PostalCodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// Official postal code catalog of Correos de México: colonias and alcaldía of each postal code.
/// </summary>
[ApiController]
[Route("codigos-postales")]
[Tags("Catálogos")]
public class PostalCodesController(PostalCodeService service) : ControllerBase
{
    /// <summary>Colonias of a postal code (5 digits), to fill the address form.</summary>
    [HttpGet("{code:length(5)}")]
    public Task<IReadOnlyList<PostalCodeDto>> Lookup(string code, CancellationToken ct) =>
        service.LookupAsync(code, ct);

    /// <summary>By postal code prefix or colonia name (at least 2 characters), up to 100 results.</summary>
    [HttpGet]
    public Task<IReadOnlyList<PostalCodeDto>> Search([FromQuery] string? search, CancellationToken ct) =>
        service.SearchAsync(search, ct);

    [HttpGet("resumen")]
    public Task<PostalCodeSummaryDto> Summary(CancellationToken ct) =>
        service.SummaryAsync(ct);

    /// <summary>
    /// Imports the TXT downloaded from correosdemexico.gob.mx (one state or all of them). SuperUser and Administración only.
    /// </summary>
    [HttpPost("importar")]
    [Authorize(Policies.Administrators)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(30_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 30_000_000)]
    public async Task<PostalCodeImportDto> Import(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return await service.ImportAsync(stream, ct);
    }
}
