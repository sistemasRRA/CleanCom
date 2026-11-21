using System.Collections.Concurrent;
using CleanCom.Application.Common;

namespace CleanCom.Application.Tracking;

/// <summary>
/// The last position of each chofer, in memory (singleton), so the live map does not read the database on every
/// reading. After a restart it is empty and the map falls back to the last saved point of the history.
/// </summary>
public class LiveLocations
{
    private readonly ConcurrentDictionary<int, LiveLocationDto> byRutaDia = new();

    public LiveLocationDto? Get(int rutaDiaId) => byRutaDia.GetValueOrDefault(rutaDiaId);

    /// <summary>Keeps the position unless a newer one is already there. Drops the days before it.</summary>
    public void Set(LiveLocationDto location)
    {
        byRutaDia.AddOrUpdate(location.RutaDiaId, location, (_, current) => current.At > location.At ? current : location);
        foreach (var old in byRutaDia.Values.Where(u => u.Date < location.Date))
        {
            byRutaDia.TryRemove(old.RutaDiaId, out _);
        }
    }
}
