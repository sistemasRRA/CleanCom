namespace CleanCom.Dominio.Comun;

/// <summary>
/// Base de toda entidad: identificador y auditoría. Las fechas se guardan en UTC.
/// </summary>
public abstract class Entidad
{
    public int Id { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTimeOffset? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}

/// <summary>
/// Entidad con baja lógica: nunca se borra, se marca como inactiva.
/// </summary>
public interface IActivable
{
    bool Activo { get; set; }
}
