using CleanCom.Dominio.Operacion;

namespace CleanCom.Aplicacion.Operacion;

/// <summary>
/// Lectura tipada de la configuración para los casos de uso.
/// </summary>
public static class ConfiguracionExtensiones
{
    public static async Task<TimeOnly> ObtenerHoraAsync(this IServicioConfiguracion s, string clave, CancellationToken ct, int? rutaId = null) =>
        ValorConfiguracion.ComoHora(await s.ObtenerAsync(clave, rutaId, ct));

    public static async Task<int> ObtenerEnteroAsync(this IServicioConfiguracion s, string clave, CancellationToken ct, int? rutaId = null) =>
        ValorConfiguracion.ComoEntero(await s.ObtenerAsync(clave, rutaId, ct));

    public static async Task<decimal> ObtenerDecimalAsync(this IServicioConfiguracion s, string clave, CancellationToken ct, int? rutaId = null) =>
        ValorConfiguracion.ComoDecimal(await s.ObtenerAsync(clave, rutaId, ct));

    public static async Task<bool> ObtenerBooleanoAsync(this IServicioConfiguracion s, string clave, CancellationToken ct, int? rutaId = null) =>
        ValorConfiguracion.ComoBooleano(await s.ObtenerAsync(clave, rutaId, ct));

    public static async Task<RangoHoras> ObtenerRangoHorasAsync(this IServicioConfiguracion s, string clave, CancellationToken ct, int? rutaId = null) =>
        ValorConfiguracion.ComoRangoHoras(await s.ObtenerAsync(clave, rutaId, ct));
}
