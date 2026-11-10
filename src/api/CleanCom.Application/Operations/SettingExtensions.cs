using CleanCom.Domain.Operations;

namespace CleanCom.Application.Operations;

/// <summary>
/// Typed reading of settings for the use cases.
/// </summary>
public static class SettingExtensions
{
    public static async Task<TimeOnly> GetTimeAsync(this ISettingService s, string key, CancellationToken ct, int? rutaId = null) =>
        SettingValue.AsTime(await s.GetAsync(key, rutaId, ct));

    public static async Task<int> GetIntegerAsync(this ISettingService s, string key, CancellationToken ct, int? rutaId = null) =>
        SettingValue.AsInteger(await s.GetAsync(key, rutaId, ct));

    public static async Task<decimal> GetDecimalAsync(this ISettingService s, string key, CancellationToken ct, int? rutaId = null) =>
        SettingValue.AsDecimal(await s.GetAsync(key, rutaId, ct));

    public static async Task<bool> GetBooleanAsync(this ISettingService s, string key, CancellationToken ct, int? rutaId = null) =>
        SettingValue.AsBoolean(await s.GetAsync(key, rutaId, ct));

    public static async Task<TimeRange> GetTimeRangeAsync(this ISettingService s, string key, CancellationToken ct, int? rutaId = null) =>
        SettingValue.AsTimeRange(await s.GetAsync(key, rutaId, ct));
}
