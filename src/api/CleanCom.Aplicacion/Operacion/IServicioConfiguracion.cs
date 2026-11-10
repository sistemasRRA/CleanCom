namespace CleanCom.Aplicacion.Operacion;

public interface IServicioConfiguracion
{
    /// <summary>Valores vigentes de todas las claves, con su siguiente cambio programado.</summary>
    Task<IReadOnlyList<ConfiguracionDto>> ListarAsync(CancellationToken ct);

    Task<IReadOnlyList<HistorialConfiguracionDto>> HistorialAsync(string? clave, CancellationToken ct);

    Task<HistorialConfiguracionDto> CambiarAsync(CambiarConfiguracionRequest request, CancellationToken ct);

    /// <summary>Valor que aplica ahora, tomando el de la ruta si la clave lo permite y la ruta lo tiene.</summary>
    Task<string> ObtenerAsync(string clave, int? rutaId, CancellationToken ct);
}
