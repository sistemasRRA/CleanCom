using static CleanCom.Dominio.Operacion.ClavesConfiguracion;
using T = CleanCom.Dominio.Operacion.TipoValorConfiguracion;

namespace CleanCom.Dominio.Operacion;

/// <param name="PorRuta">Puede sobrescribirse por ruta (marcado con (R) en las reglas).</param>
public record DefinicionConfiguracion(string Clave, TipoValorConfiguracion Tipo, string ValorInicial, string Descripcion, bool PorRuta = false);

/// <summary>
/// Claves válidas con su tipo y valor inicial, tal como vienen en docs/03-reglas-de-negocio.md.
/// </summary>
public static class CatalogoConfiguracion
{
    public static IReadOnlyList<DefinicionConfiguracion> Definiciones { get; } =
    [
        new(AgendaCorteMismoDia, T.Hora, "13:00", "Después de esta hora ya no entra el mismo día"),
        new(AgendaCorteCambioDia, T.Hora, "12:00", "Cambio de día desde la app del invitado"),
        new(AgendaCorteCancelacion, T.Hora, "13:00", "Cancelación desde la app del invitado, hasta esta hora del día anterior a la recolección"),
        new(RutaSalidaBase, T.Hora, "09:00", "Salida de Leones"),
        new(RutaRegresoMaximo, T.Hora, "18:00", "Fin de ruta y alerta de horas extra", PorRuta: true),
        new(RutaRegresoMaximoSabado, T.Hora, "14:00", "Fin de ruta en sábado", PorRuta: true),
        new(TurnoManana, T.RangoHoras, "09:00-13:00", "Turno de la mañana"),
        new(TurnoTarde, T.RangoHoras, "13:00-18:00", "Turno de la tarde"),
        new(ComidaMinutos, T.Entero, "60", "Duración de la comida dentro de la ruta"),
        new(ComidaDesde, T.Hora, "13:00", "Hora a partir de la cual se toma la comida"),
        new(RutaMetaRecolecciones, T.Entero, "10", "Meta de recolecciones por ruta al día", PorRuta: true),
        new(RutaCupoParadas, T.Entero, "14", "Paradas máximas por ruta al día; las fijas cuentan", PorRuta: true),
        new(UnidadCapacidadPrendas, T.Entero, "140", "Prendas que caben en la unidad; aviso en planeación"),
        new(PlantaDiasProceso, T.Entero, "3", "Días de proceso cuando la línea de servicio no tiene los suyos"),
        new(ParadaMinutosRecoleccion, T.Entero, "15", "Minutos por recolección para horas estimadas"),
        new(ParadaMinutosEntrega, T.Entero, "10", "Minutos por entrega para horas estimadas"),
        new(ParadaMinutosTraslado, T.Entero, "12", "Minutos por traslado o insumos"),
        new(GpsToleranciaMetros, T.Entero, "300", "Distancia máxima para validar llegada y evidencia"),
        new(VisitaFotoObligatoria, T.Booleano, "true", "Foto obligatoria en cada visita"),
        new(VisitaOrdenObligatorio, T.Booleano, "true", "El chofer registra las paradas en el orden asignado"),
        new(FijasDiasAnticipacion, T.Entero, "7", "Días hacia adelante que se generan las paradas fijas"),
        new(CrmDiasNuevo, T.Entero, "45", "Días desde el primer servicio para considerar nuevo al invitado"),
        new(CrmDiasInactivo, T.Entero, "120", "Días sin servicio para inactivo; también define recuperado y perdido"),
        new(CrmFactorEnRiesgo, T.Decimal, "2", "Veces su frecuencia sin pedir para estar en riesgo"),
        new(BaseDireccion, T.Texto, "Calz. de los Leones 165, Los Alpes, Álvaro Obregón, 01710", "Dirección de la base, inicio y fin de ruta"),
        new(BaseUbicacion, T.Coordenada, "19.3608092,-99.1968494", "Coordenadas de la base"),
    ];

    private static readonly Dictionary<string, DefinicionConfiguracion> PorClave =
        Definiciones.ToDictionary(d => d.Clave, StringComparer.Ordinal);

    public static DefinicionConfiguracion? Buscar(string clave) => PorClave.GetValueOrDefault(clave);
}
