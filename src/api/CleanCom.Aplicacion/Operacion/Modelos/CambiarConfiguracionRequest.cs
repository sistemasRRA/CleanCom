namespace CleanCom.Aplicacion.Operacion;

/// <param name="VigenteDesde">Vacío = de inmediato. Una fecha futura programa el cambio.</param>
public record CambiarConfiguracionRequest(string Clave, string Valor, DateTimeOffset? VigenteDesde, string? Nota);
