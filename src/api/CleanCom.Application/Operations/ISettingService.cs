namespace CleanCom.Application.Operations;

public interface ISettingService
{
    /// <summary>Current values of every key, with their next scheduled change.</summary>
    Task<IReadOnlyList<SettingDto>> ListAsync(CancellationToken ct);

    Task<IReadOnlyList<SettingHistoryDto>> HistoryAsync(string? key, CancellationToken ct);

    Task<SettingHistoryDto> ChangeAsync(ChangeSettingRequest request, CancellationToken ct);

    /// <summary>Value that applies now, taking the ruta's own value when the key allows it and the ruta has one.</summary>
    Task<string> GetAsync(string key, int? rutaId, CancellationToken ct);
}
