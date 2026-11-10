namespace CleanCom.Application.Operations;

public record SettingHistoryDto(int Id, string Key, string Value, DateTimeOffset EffectiveFrom, DateTimeOffset CreatedAt, int? CreatedBy, string? Note);
