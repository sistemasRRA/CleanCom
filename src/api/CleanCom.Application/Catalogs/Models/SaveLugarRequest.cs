using CleanCom.Domain.Catalogs;

namespace CleanCom.Application.Catalogs;

public record SaveLugarRequest(string Name, LugarType Type, string Address, double? Lat, double? Lng, string? OpeningHours, bool Active = true);
