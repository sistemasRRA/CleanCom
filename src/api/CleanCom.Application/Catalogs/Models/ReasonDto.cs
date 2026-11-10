using CleanCom.Domain.Catalogs;

namespace CleanCom.Application.Catalogs;

public record ReasonDto(int Id, ReasonType Type, string Text, bool Active);
