namespace CleanCom.Aplicacion.Operacion;

public record HistorialConfiguracionDto(int Id, string Clave, string Valor, DateTimeOffset VigenteDesde, DateTimeOffset CreadoEn, int? CreadoPor, string? Nota);
