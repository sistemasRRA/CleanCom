using CleanCom.Dominio.Catalogos;
using CleanCom.Dominio.Comun;

namespace CleanCom.Dominio.Operacion;

/// <summary>
/// Decide qué valor de configuración aplica en un momento dado.
/// </summary>
public static class ResolutorConfiguracion
{
    /// <summary>
    /// El registro vigente: el de <see cref="Configuracion.VigenteDesde"/> más reciente que ya empezó.
    /// Los cambios programados a futuro no aplican hasta su fecha.
    /// </summary>
    public static Configuracion? Vigente(IEnumerable<Configuracion> historial, string clave, DateTimeOffset ahoraUtc) =>
        historial
            .Where(c => c.Clave == clave && c.VigenteDesde <= ahoraUtc)
            .OrderByDescending(c => c.VigenteDesde)
            .ThenByDescending(c => c.Id)
            .FirstOrDefault();

    /// <summary>
    /// El siguiente cambio programado, si lo hay.
    /// </summary>
    public static Configuracion? Programado(IEnumerable<Configuracion> historial, string clave, DateTimeOffset ahoraUtc) =>
        historial
            .Where(c => c.Clave == clave && c.VigenteDesde > ahoraUtc)
            .OrderBy(c => c.VigenteDesde)
            .ThenByDescending(c => c.Id)
            .FirstOrDefault();

    /// <summary>
    /// Valor que aplica. Si la clave admite valor por ruta y la ruta lo tiene, gana el de la ruta.
    /// </summary>
    public static string Resolver(IEnumerable<Configuracion> historial, string clave, DateTimeOffset ahoraUtc, Ruta? ruta = null)
    {
        var definicion = CatalogoConfiguracion.Buscar(clave)
            ?? throw new ExcepcionDominio($"La clave de configuración \"{clave}\" no existe.");

        if (definicion.PorRuta && ruta?.ValorPropio(clave) is { } propio)
        {
            return propio;
        }

        return Vigente(historial, clave, ahoraUtc)?.Valor
            ?? throw new ExcepcionDominio($"La configuración \"{clave}\" no tiene valor vigente.");
    }
}
