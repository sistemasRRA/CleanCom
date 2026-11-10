using CleanCom.Domain.Operations;

namespace CleanCom.Application.Operations;

public record SettingDto(
    string Key,
    string Description,
    SettingValueType Type,
    bool PerRuta,
    string Value,
    DateTimeOffset EffectiveFrom,
    ScheduledChangeDto? Scheduled);
