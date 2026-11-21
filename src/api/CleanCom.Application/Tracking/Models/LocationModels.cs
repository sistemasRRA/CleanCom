namespace CleanCom.Application.Tracking;

/// <summary>
/// GPS readings of the chofer's phone, sent every 30 to 60 seconds during the shift. Without signal the app keeps them
/// and sends them together when the connection is back, so it is a list.
/// </summary>
public record ChoferLocationsRequest(IReadOnlyList<LocationReadingRequest> Readings);

/// <param name="At">When the phone took it; now when empty.</param>
/// <param name="AccuracyMeters">Accuracy the phone reported.</param>
public record LocationReadingRequest(double Lat, double Lng, DateTimeOffset? At = null, double? AccuracyMeters = null);

/// <param name="Accepted">Whether the readings were used. False when the chofer has no ruta today or none was usable.</param>
/// <param name="Notice">Why they were not used, in Spanish.</param>
public record LocationResultDto(bool Accepted, string? Notice = null);

/// <summary>Where the chofer went on a RutaDia (history), to compare with the planned ruta.</summary>
public record TrackDto(int RutaDiaId, DateOnly Date, IReadOnlyList<TrackPointDto> Points);

public record TrackPointDto(double Lat, double Lng, DateTimeOffset At);
