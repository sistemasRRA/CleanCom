namespace CleanCom.Application.Operations;

/// <param name="EffectiveFrom">Empty = right away. A future date schedules the change.</param>
public record ChangeSettingRequest(string Key, string Value, DateTimeOffset? EffectiveFrom, string? Note);
