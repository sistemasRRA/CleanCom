using CleanCom.Domain.Common;

namespace CleanCom.Domain.Routing;

/// <summary>
/// Reduced history of where the chofer was during a RutaDia (see <see cref="LocationTrail"/>). The last position is
/// also kept in memory for the live map. A reading is a fact, so it is never edited nor deleted.
/// </summary>
public class ChoferLocation : Entity
{
    public int RutaDiaId { get; set; }
    public RutaDia? RutaDia { get; set; }
    public NetTopologySuite.Geometries.Point Location { get; set; } = null!;
    /// <summary>When the phone took it (UTC); it may reach the API later, sin señal.</summary>
    public DateTimeOffset RecordedAt { get; set; }
    /// <summary>Accuracy the phone reported, in meters; null when unknown.</summary>
    public int? AccuracyMeters { get; set; }
}
