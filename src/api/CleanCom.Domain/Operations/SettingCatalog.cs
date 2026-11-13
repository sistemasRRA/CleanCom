using static CleanCom.Domain.Operations.SettingKeys;
using T = CleanCom.Domain.Operations.SettingValueType;

namespace CleanCom.Domain.Operations;

/// <param name="PerRuta">Can be overridden per ruta (marked (R) in the business rules).</param>
/// <param name="Description">Shown on the Operación screen, so it is written in Spanish.</param>
public record SettingDefinition(string Key, SettingValueType Type, string InitialValue, string Description, bool PerRuta = false);

/// <summary>
/// Valid keys with their type and initial value, as listed in docs/03-reglas-de-negocio.md.
/// </summary>
public static class SettingCatalog
{
    public static IReadOnlyList<SettingDefinition> Definitions { get; } =
    [
        new(AgendaCorteSameDay, T.Time, "13:00", "Después de esta hora ya no entra el mismo día"),
        new(AgendaCorteDayChange, T.Time, "12:00", "Cambio de día desde la app del invitado"),
        new(AgendaCorteCancellation, T.Time, "13:00", "Cancelación desde la app del invitado, hasta esta hora del día anterior a la recolección"),
        new(RutaBaseDeparture, T.Time, "09:00", "Salida de Leones"),
        new(RutaMaxReturnTime, T.Time, "18:00", "Fin de ruta y alerta de horas extra", PerRuta: true),
        new(RutaMaxReturnTimeSaturday, T.Time, "14:00", "Fin de ruta en sábado", PerRuta: true),
        new(TurnoMorning, T.TimeRange, "09:00-13:00", "Turno de la mañana"),
        new(TurnoAfternoon, T.TimeRange, "13:00-18:00", "Turno de la tarde"),
        new(LunchMinutes, T.Integer, "60", "Duración de la comida dentro de la ruta"),
        new(LunchFrom, T.Time, "13:00", "Hora a partir de la cual se toma la comida"),
        new(RutaRecoleccionGoal, T.Integer, "10", "Meta de recolecciones por ruta al día", PerRuta: true),
        new(RutaCupoParadas, T.Integer, "14", "Paradas máximas por ruta al día; las fijas cuentan", PerRuta: true),
        new(UnidadGarmentCapacity, T.Integer, "140", "Prendas que caben en la unidad; aviso en planeación"),
        new(PlantaProcessingDays, T.Integer, "3", "Días de proceso cuando la línea de servicio no tiene los suyos"),
        new(ParadaRecoleccionMinutes, T.Integer, "15", "Minutos por recolección para horas estimadas"),
        new(ParadaEntregaMinutes, T.Integer, "10", "Minutos por entrega para horas estimadas"),
        new(ParadaTransferMinutes, T.Integer, "12", "Minutos por traslado o insumos"),
        new(GpsToleranceMeters, T.Integer, "300", "Distancia máxima para validar llegada y evidencia"),
        new(VisitPhotoRequired, T.Boolean, "true", "Foto obligatoria en cada visita"),
        new(VisitOrderRequired, T.Boolean, "true", "El chofer registra las paradas en el orden asignado"),
        new(FixedDaysAhead, T.Integer, "7", "Días hacia adelante que se generan las paradas fijas"),
        new(CrmNewDays, T.Integer, "45", "Días desde el primer servicio para considerar nuevo al invitado"),
        new(CrmInactiveDays, T.Integer, "120", "Días sin servicio para inactivo; también define recuperado y perdido"),
        new(CrmAtRiskFactor, T.Decimal, "2", "Veces su frecuencia sin pedir para estar en riesgo"),
        new(BaseAddress, T.Text, "Calz. de los Leones 165, Los Alpes, Álvaro Obregón, 01710", "Dirección de la base, inicio y fin de ruta"),
        new(BaseLocation, T.Coordinate, "19.3608092,-99.1968494", "Coordenadas de la base"),
        // New keys go last: the seed gives each definition the id of its position.
        new(CrmNoAnswerDays, T.Integer, "1", "Días para volver a llamar si no contestó"),
        new(CrmCallLaterDays, T.Integer, "3", "Días para volver a llamar si pidió que le llamaran después"),
        new(CrmNotNowDays, T.Integer, "21", "Días para volver a llamar si dijo que no por ahora"),
    ];

    private static readonly Dictionary<string, SettingDefinition> ByKey =
        Definitions.ToDictionary(d => d.Key, StringComparer.Ordinal);

    public static SettingDefinition? Find(string key) => ByKey.GetValueOrDefault(key);
}
