using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CleanCom.Application.Geography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CleanCom.Infrastructure.Geography;

/// <summary>
/// Geocoding with OpenStreetMap's Nominatim while the Google Cloud keys are pending. Its usage policy allows at most
/// one request per second and an identifying User-Agent (Geocoding:UserAgent), so requests are spaced out and
/// results are cached. Tries from the most precise query to the least: street and number, street, colonia, postal code.
/// </summary>
internal sealed class NominatimGeocoder(HttpClient http, IMemoryCache cache, ILogger<NominatimGeocoder> logger) : IGeocoder
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTimeOffset lastRequest = DateTimeOffset.MinValue;
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1.1);

    public async Task<GeocodeResult?> GeocodeAsync(GeocodeQuery q, CancellationToken ct)
    {
        var street = Clean(q.Street);
        var number = Clean(q.ExteriorNumber);
        var neighborhood = Clean(q.Neighborhood);
        var borough = Clean(q.Borough);
        var postalCode = Clean(q.PostalCode);
        if (street is null && neighborhood is null && postalCode is null)
        {
            return null;
        }

        var key = $"geocode|{street}|{number}|{neighborhood}|{borough}|{postalCode}".ToLowerInvariant();
        if (cache.TryGetValue(key, out GeocodeResult? cached))
        {
            return cached;
        }

        var result = await SearchAsync(street, number, neighborhood, borough, postalCode, ct);
        // A miss is cached for less time: the office may fix a typo and the map data changes.
        cache.Set(key, result, result is null ? TimeSpan.FromHours(1) : TimeSpan.FromDays(7));
        return result;
    }

    private async Task<GeocodeResult?> SearchAsync(string? street, string? number, string? neighborhood, string? borough, string? postalCode, CancellationToken ct)
    {
        if (street is not null)
        {
            var withNumber = Join(number is null ? street : $"{street} {number}", neighborhood, borough, postalCode);
            if (await FreeFormAsync(withNumber, ct) is { } exact)
            {
                return exact;
            }
            // Colonia names in the map often differ from the official catalog; try without it.
            if (neighborhood is not null && await FreeFormAsync(Join(street, borough, postalCode), ct) is { } byStreet)
            {
                return byStreet;
            }
        }
        if (neighborhood is not null && await FreeFormAsync(Join(neighborhood, borough, postalCode), ct) is { } byColonia)
        {
            return byColonia with { Precision = Coarser(byColonia.Precision, GeocodePrecision.Colonia) };
        }
        if (postalCode is not null && await RequestAsync($"search?postalcode={Uri.EscapeDataString(postalCode)}&countrycodes=mx&format=jsonv2&addressdetails=1&limit=1", ct) is { } byPostalCode)
        {
            return byPostalCode with { Precision = GeocodePrecision.CodigoPostal };
        }
        return null;
    }

    private Task<GeocodeResult?> FreeFormAsync(string text, CancellationToken ct) =>
        RequestAsync($"search?q={Uri.EscapeDataString(text)}&countrycodes=mx&format=jsonv2&addressdetails=1&limit=1&accept-language=es", ct);

    private async Task<GeocodeResult?> RequestAsync(string pathAndQuery, CancellationToken ct)
    {
        await Gate.WaitAsync(ct);
        try
        {
            var wait = lastRequest + MinInterval - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, ct);
            }
            lastRequest = DateTimeOffset.UtcNow;

            var places = await http.GetFromJsonAsync<Place[]>(pathAndQuery, ct);
            return places is { Length: > 0 } ? ToResult(places[0]) : null;
        }
        catch (Exception e) when (e is HttpRequestException or System.Text.Json.JsonException || (e is TaskCanceledException && !ct.IsCancellationRequested))
        {
            // The map is a help: when the service is down the office places the pin by hand.
            logger.LogWarning(e, "Nominatim did not answer {Query}", pathAndQuery);
            return null;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static GeocodeResult? ToResult(Place p)
    {
        if (!double.TryParse(p.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || !double.TryParse(p.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
        {
            return null;
        }
        var precision = p.Address?.HouseNumber is not null ? GeocodePrecision.Direccion
            : p.Category == "highway" || p.AddressType == "road" ? GeocodePrecision.Calle
            : p.AddressType == "postcode" ? GeocodePrecision.CodigoPostal
            : GeocodePrecision.Colonia;
        return new GeocodeResult(Math.Round(lat, 6), Math.Round(lng, 6), precision);
    }

    private static GeocodePrecision Coarser(GeocodePrecision a, GeocodePrecision b) => a > b ? a : b;

    private static string Join(params string?[] parts) =>
        string.Join(", ", parts.Where(x => x is not null).Append("México"));

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private sealed record Place(
        [property: JsonPropertyName("lat")] string? Lat,
        [property: JsonPropertyName("lon")] string? Lon,
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("addresstype")] string? AddressType,
        [property: JsonPropertyName("address")] PlaceAddress? Address);

    private sealed record PlaceAddress([property: JsonPropertyName("house_number")] string? HouseNumber);
}
