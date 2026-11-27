using CleanCom.Domain.Catalogs;

namespace CleanCom.Application.Catalogs;

/// <param name="IsDigital">Only for "Cómo nos conoció": the source counts as marketing digital (Clientes y fuentes).</param>
public record SaveReasonRequest(ReasonType Type, string Text, bool Active = true, bool IsDigital = false);
