using CleanCom.Application.Chofer;
using CleanCom.Application.Common;

namespace CleanCom.Application.Tracking;

/// <summary>Vista del chofer: what the chofer of a ruta sees, with where they are now.</summary>
/// <param name="Location">Last known position of that ruta's chofer that day; null without readings.</param>
public record ChoferPreviewDto(ChoferRutaDto Ruta, LiveLocationDto? Location);

public class ChoferPreviewService(ChoferService chofer, LocationService locations)
{
    public async Task<ChoferPreviewDto> GetAsync(int rutaId, DateOnly? date, CancellationToken ct)
    {
        var view = await chofer.PreviewAsync(rutaId, date, ct);
        var location = (await locations.LiveAsync(view.Date, ct)).FirstOrDefault(l => l.RutaId == rutaId);
        return new ChoferPreviewDto(view, location);
    }
}
