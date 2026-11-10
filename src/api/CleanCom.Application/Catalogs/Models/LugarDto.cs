using CleanCom.Domain.Catalogs;

namespace CleanCom.Application.Catalogs;

public record LugarDto(int Id, string Name, LugarType Type, string Address, double? Lat, double? Lng, string? OpeningHours, bool Active);
