using CleanCom.Dominio.Tiempo;

namespace CleanCom.Infraestructura.Tiempo;

/// <summary>
/// Reloj real. En pruebas se le pasa un <see cref="TimeProvider"/> con la hora que se quiera.
/// </summary>
public class RelojNegocio(TimeProvider proveedor) : IRelojNegocio
{
    public DateTimeOffset AhoraUtc => proveedor.GetUtcNow();

    public DateTimeOffset AhoraLocal => HoraNegocio.ALocal(AhoraUtc);

    public DateOnly Hoy => HoraNegocio.Fecha(AhoraUtc);
}
