namespace CleanCom.Domain.Kpis;

public enum KpiArea
{
    Logistica,
    Venta,
    Experiencia,
}

public enum KpiUnit
{
    Numero,
    Porcentaje,
    Pesos,
    /// <summary>NPS: from −100 to 100.</summary>
    Puntos,
}

/// <summary>How the facts of a period become its value (see <see cref="KpiRules.Value"/>).</summary>
public enum KpiAggregation
{
    /// <summary>Sum of numerators over sum of denominators (an average or a share).</summary>
    Ratio,
    /// <summary>Sum of numerators (a count or an amount).</summary>
    Sum,
    /// <summary>How many different keys (invitados) have a numerator.</summary>
    DistinctCount,
    /// <summary>Sum of numerators over the number of different keys (services per invitado).</summary>
    PerDistinct,
}

/// <summary>How a goal written for a day, a week or a month applies to the period shown.</summary>
public enum KpiGoalScaling
{
    /// <summary>An average or a share: the goal applies as written to any period.</summary>
    None,
    /// <summary>A count or an amount that adds up day by day: the goal is scaled by days.</summary>
    ByDays,
    /// <summary>
    /// Different invitados: they do not add up (the same invitado comes back), so the goal only applies to a period of its
    /// own kind (a weekly goal to weeks, a monthly one to months).
    /// </summary>
    SamePeriod,
}

/// <param name="Key">Stored with the goals and used in the URL (kebab-case Spanish, it is data).</param>
/// <param name="Calculation">How it is calculated, in Spanish, for the screen.</param>
/// <param name="Multiplier">100 for percentages and NPS.</param>
/// <param name="Source">Where the data comes from when it is not the system itself, or why it has no data yet.</param>
public record KpiDefinition(
    string Key,
    string Name,
    KpiArea Area,
    KpiUnit Unit,
    bool HigherIsBetter,
    KpiAggregation Aggregation,
    string Calculation,
    decimal Multiplier = 1,
    string? Source = null,
    KpiGoalScaling? GoalScaling = null)
{
    /// <summary>
    /// Sums scale by days, distinct counts only compare with a period of their kind, averages and shares apply as
    /// written; an indicator may say otherwise (clientes nuevos add up: each invitado is new only once).
    /// </summary>
    public KpiGoalScaling Scaling => GoalScaling ?? Aggregation switch
    {
        KpiAggregation.Sum => KpiGoalScaling.ByDays,
        KpiAggregation.DistinctCount => KpiGoalScaling.SamePeriod,
        _ => KpiGoalScaling.None,
    };
}

/// <summary>
/// The indicators of the sheet "KPI's 2026 CleanCom" (docs/03-reglas-de-negocio.md, KPIs). The formulas are code; the
/// goals (rojo, verde, súper verde, responsable) are data with vigencia (<see cref="KpiGoal"/>).
/// </summary>
public static class KpiCatalog
{
    public const string RecoleccionesPorRutaDia = "recolecciones-por-ruta-dia";
    public const string PrendasPorTicket = "prendas-por-ticket";
    public const string CobroAnticipado = "cobro-anticipado";
    public const string ClientesNuevos = "clientes-nuevos";
    public const string ClientesAtendidos = "clientes-atendidos";
    public const string TicketsFirmados = "tickets-firmados";
    public const string VisitasConFoto = "visitas-con-foto";
    public const string PedidosFueraDeTiempo = "pedidos-fuera-de-tiempo";
    public const string LlegadasATiempo = "llegadas-a-tiempo";
    public const string Nps = "nps";
    public const string EncuestasRespondidas = "encuestas-respondidas";
    public const string ToquesWhatsApp = "toques-whatsapp";
    public const string FueraDeCobertura = "servicios-fuera-de-cobertura";
    public const string TicketPromedio = "ticket-promedio";
    public const string FacturacionSinConvenios = "facturacion-sin-convenios";
    public const string Recurrencia = "recurrencia";

    private const string WaitsForSurvey = "Llega con la encuesta NPS de la app del invitado.";
    private const string FromPos = "Del POS; mientras no está conectado, de los tickets capturados en Tickets en planta con importe.";

    public static readonly IReadOnlyList<KpiDefinition> All =
    [
        new(RecoleccionesPorRutaDia, "Recolecciones por ruta al día", KpiArea.Logistica, KpiUnit.Numero, true, KpiAggregation.Ratio,
            "Recolecciones realizadas / días operados de cada ruta."),
        new(PrendasPorTicket, "Prendas por ticket (fuera de convenio)", KpiArea.Logistica, KpiUnit.Numero, true, KpiAggregation.Ratio,
            "Prendas / tickets de clientes finales (sin convenio, empresa ni tienda interna).", Source: FromPos),
        new(CobroAnticipado, "Cobro anticipado", KpiArea.Logistica, KpiUnit.Porcentaje, true, KpiAggregation.Ratio,
            "Pedidos entregados que se cobraron en la recolección / pedidos entregados.", 100),
        new(ClientesNuevos, "Clientes nuevos", KpiArea.Logistica, KpiUnit.Numero, true, KpiAggregation.DistinctCount,
            "Invitados cuyo primer servicio (recolección realizada) cayó en el periodo.", GoalScaling: KpiGoalScaling.ByDays),
        new(ClientesAtendidos, "Clientes atendidos", KpiArea.Logistica, KpiUnit.Numero, true, KpiAggregation.DistinctCount,
            "Invitados distintos con una visita realizada."),
        new(TicketsFirmados, "Tickets firmados", KpiArea.Logistica, KpiUnit.Porcentaje, true, KpiAggregation.Ratio,
            "Entregas realizadas con firma / entregas realizadas.", 100),
        new(VisitasConFoto, "Visitas con foto de evidencia", KpiArea.Logistica, KpiUnit.Porcentaje, true, KpiAggregation.Ratio,
            "Visitas realizadas con foto / visitas realizadas.", 100),
        new(PedidosFueraDeTiempo, "Pedidos entregados fuera de tiempo", KpiArea.Logistica, KpiUnit.Numero, false, KpiAggregation.Sum,
            "Pedidos entregados después de la primera fecha de entrega que se les dio."),
        new(LlegadasATiempo, "Llegadas a tiempo", KpiArea.Logistica, KpiUnit.Porcentaje, true, KpiAggregation.Ratio,
            "Llegadas dentro de la tolerancia de la hora estimada del plan / llegadas.", 100),
        new(Nps, "NPS", KpiArea.Logistica, KpiUnit.Puntos, true, KpiAggregation.Ratio,
            "% promotores (9 a 10) − % detractores (0 a 6).", 100, WaitsForSurvey),
        new(EncuestasRespondidas, "Encuestas respondidas / enviadas", KpiArea.Logistica, KpiUnit.Porcentaje, true, KpiAggregation.Ratio,
            "Encuestas respondidas / encuestas enviadas.", 100, WaitsForSurvey),
        new(ToquesWhatsApp, "Toques a clientes por WhatsApp", KpiArea.Logistica, KpiUnit.Numero, true, KpiAggregation.Sum,
            "Avisos de WhatsApp enviados (o mandados a mano) más contactos del CRM registrados."),
        new(FueraDeCobertura, "Servicios fuera de cobertura", KpiArea.Logistica, KpiUnit.Numero, false, KpiAggregation.Sum,
            "Visitas realizadas a direcciones fuera de todo polígono de zona."),
        new(TicketPromedio, "Ticket promedio", KpiArea.Venta, KpiUnit.Pesos, true, KpiAggregation.Ratio,
            "Importe de los tickets / tickets.", Source: FromPos),
        new(FacturacionSinConvenios, "Facturación rutas sin convenios", KpiArea.Venta, KpiUnit.Pesos, true, KpiAggregation.Sum,
            "Importe de los tickets de clientes finales.", Source: FromPos),
        new(Recurrencia, "Recurrencia", KpiArea.Experiencia, KpiUnit.Numero, true, KpiAggregation.PerDistinct,
            "Servicios (recolecciones realizadas) / invitados distintos atendidos."),
    ];

    public static KpiDefinition? Find(string key) => All.FirstOrDefault(k => k.Key == key);
}
