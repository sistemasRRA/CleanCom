using CleanCom.Api.Common;
using CleanCom.Application.Geography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCom.Api.Controllers;

/// <summary>
/// Helpers for capturing an address. Saving it stays under /invitados/{id}/direcciones.
/// </summary>
[ApiController]
[Route("direcciones")]
[Tags("Invitados")]
public class AddressesController(IGeocoder geocoder) : ControllerBase
{
    /// <summary>
    /// Coordinates of a typed address, to place the pin on the map of the address form. The office moves the pin
    /// to the house; the coordinates that decide the zona are the ones saved with the address.
    /// </summary>
    [HttpGet("ubicar")]
    [Authorize(Policies.CapturesAddresses)]
    public async Task<UbicacionDto> Locate(
        [FromQuery] string? street,
        [FromQuery] string? exteriorNumber,
        [FromQuery] string? neighborhood,
        [FromQuery] string? borough,
        [FromQuery] string? postalCode,
        CancellationToken ct)
    {
        var found = await geocoder.GeocodeAsync(new GeocodeQuery(street, exteriorNumber, neighborhood, borough, postalCode), ct);
        return found is null
            ? new UbicacionDto(false, null, null, null)
            : new UbicacionDto(true, found.Lat, found.Lng, found.Precision);
    }
}
