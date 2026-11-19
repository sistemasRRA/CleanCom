namespace CleanCom.Application.Geography;

/// <summary>How close the found point is to the house: the office moves the pin when it is not the exact address.</summary>
public enum GeocodePrecision
{
    /// <summary>The street and exterior number were found.</summary>
    Direccion,
    /// <summary>Only the street (the number is not on the map).</summary>
    Calle,
    /// <summary>Only the colonia.</summary>
    Colonia,
    /// <summary>Only the postal code.</summary>
    CodigoPostal,
}

public record GeocodeQuery(string? Street, string? ExteriorNumber, string? Neighborhood, string? Borough, string? PostalCode);

public record GeocodeResult(double Lat, double Lng, GeocodePrecision Precision);

/// <summary>
/// Address → coordinates for the map in the address form. Today OpenStreetMap (Nominatim); Google Geocoding
/// replaces it when the Google Cloud keys exist, without changing the screen.
/// </summary>
public interface IGeocoder
{
    /// <summary>The best point found, or null when nothing matches (or the service is not reachable).</summary>
    Task<GeocodeResult?> GeocodeAsync(GeocodeQuery query, CancellationToken ct);
}

/// <param name="Found">False when the address is not on the map: the office places the pin by hand.</param>
public record UbicacionDto(bool Found, double? Lat, double? Lng, GeocodePrecision? Precision);
