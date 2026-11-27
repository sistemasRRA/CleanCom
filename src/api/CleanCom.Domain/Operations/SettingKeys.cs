namespace CleanCom.Domain.Operations;

/// <summary>
/// Setting keys. The string values are data (stored in Settings and listed in
/// docs/03-reglas-de-negocio.md#configuración), so they keep their Spanish names.
/// </summary>
public static class SettingKeys
{
    public const string AgendaCorteSameDay = "Agenda.CorteMismoDia";
    public const string AgendaCorteDayChange = "Agenda.CorteCambioDia";
    public const string AgendaCorteCancellation = "Agenda.CorteCancelacion";
    public const string RutaBaseDeparture = "Ruta.SalidaBase";
    public const string RutaMaxReturnTime = "Ruta.RegresoMaximo";
    public const string RutaMaxReturnTimeSaturday = "Ruta.RegresoMaximoSabado";
    public const string TurnoMorning = "Turno.Manana";
    public const string TurnoAfternoon = "Turno.Tarde";
    public const string LunchMinutes = "Comida.Minutos";
    public const string LunchFrom = "Comida.Desde";
    public const string RutaRecoleccionGoal = "Ruta.MetaRecolecciones";
    public const string RutaCupoParadas = "Ruta.CupoParadas";
    public const string UnidadGarmentCapacity = "Unidad.CapacidadPrendas";
    public const string PlantaProcessingDays = "Planta.DiasProceso";
    public const string ParadaRecoleccionMinutes = "Parada.MinutosRecoleccion";
    public const string ParadaEntregaMinutes = "Parada.MinutosEntrega";
    public const string ParadaTransferMinutes = "Parada.MinutosTraslado";
    public const string GpsToleranceMeters = "Gps.ToleranciaMetros";
    public const string VisitPhotoRequired = "Visita.FotoObligatoria";
    public const string VisitOrderRequired = "Visita.OrdenObligatorio";
    public const string FixedDaysAhead = "Fijas.DiasAnticipacion";
    public const string CrmNewDays = "Crm.DiasNuevo";
    public const string CrmInactiveDays = "Crm.DiasInactivo";
    public const string CrmAtRiskFactor = "Crm.FactorEnRiesgo";
    public const string CrmNoAnswerDays = "Crm.DiasNoContesto";
    public const string CrmCallLaterDays = "Crm.DiasLlamarDespues";
    public const string CrmNotNowDays = "Crm.DiasNoPorAhora";
    public const string RutaAverageSpeed = "Ruta.VelocidadPromedio";
    public const string RutaDistanceFactor = "Ruta.FactorDistancia";
    public const string ParadaSpecificTimeWindow = "Parada.VentanaHoraEspecifica";
    public const string BaseAddress = "Base.Direccion";
    public const string BaseLocation = "Base.Ubicacion";
    public const string GpsHistorySeconds = "Gps.HistorialSegundos";
    public const string GpsHistoryMeters = "Gps.HistorialMetros";
    public const string UnidadServiceEveryKm = "Unidad.ServicioCadaKm";
    public const string UnidadServiceEveryMonths = "Unidad.ServicioCadaMeses";
    public const string ParadaArrivalToleranceMinutes = "Parada.ToleranciaLlegadaMinutos";
    public const string GpsNoSignalMinutes = "Gps.SinSenalMinutos";
    public const string AccessCodeMinutes = "Acceso.CodigoMinutos";
    public const string AccessCodeAttempts = "Acceso.CodigoIntentos";
    public const string AccessCodeResendSeconds = "Acceso.CodigoReenvioSegundos";
    public const string AccessCodesPerHour = "Acceso.CodigosPorHora";
    public const string PromocionesSellosGoal = "Promociones.MetaCanjeSellos";
    public const string PromocionesCuponeraGoal = "Promociones.MetaCanjeCuponera";
    public const string PromocionesSelloCap = "Promociones.TopeSello";
    public const string UnidadMaxKmPerShift = "Unidad.KmMaximoPorTurno";
    public const string AppInvitadoInvitation = "AppInvitado.MensajeInvitacion";
}
