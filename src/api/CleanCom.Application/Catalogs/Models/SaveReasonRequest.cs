using CleanCom.Domain.Catalogs;

namespace CleanCom.Application.Catalogs;

public record SaveReasonRequest(ReasonType Type, string Text, bool Active = true);
