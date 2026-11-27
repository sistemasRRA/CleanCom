using CleanCom.Domain.Catalogs;

namespace CleanCom.Application.Catalogs;

/// <param name="IsDigital">"Cómo nos conoció" that counts as marketing digital (Clientes y fuentes).</param>
public record ReasonDto(int Id, ReasonType Type, string Text, bool Active, bool IsDigital = false);
