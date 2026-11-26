using CleanCom.Domain.Kpis;

namespace CleanCom.Application.Kpis;

public enum KpiTrend
{
    SinDatos,
    Sube,
    Baja,
    Igual,
}

/// <summary>The KPIs screen for one period (docs/pantallas/oficina/kpis.md).</summary>
/// <param name="CalculatedAt">When the saved values were calculated (the nightly job or "Recalcular").</param>
/// <param name="ExternalSources">Indicators that are not calculated here.</param>
public record KpiBoardDto(
    KpiPeriod Period,
    DateOnly Start,
    DateOnly End,
    string Label,
    bool IsCurrent,
    string PreviousLabel,
    DateTimeOffset? CalculatedAt,
    KpiLightCountsDto Counts,
    IReadOnlyList<KpiRutaDto> Rutas,
    IReadOnlyList<KpiCardDto> Indicadores,
    string ExternalSources);

public record KpiLightCountsDto(int SuperVerde, int Verde, int Rojo, int SinMeta);

public record KpiRutaDto(int Id, string Code, string? Color);

/// <param name="Previous">The value of the period before.</param>
/// <param name="ByRuta">Desglose by ruta, each with the light of its own goal (or the company's, for averages and shares).</param>
/// <param name="GoalNote">Why the goal does not apply to this period (a weekly goal of different invitados seen by month).</param>
public record KpiCardDto(
    string Key,
    string Name,
    KpiArea Area,
    KpiUnit Unit,
    bool HigherIsBetter,
    string Calculation,
    string? Source,
    decimal? Value,
    decimal? Previous,
    KpiTrend Trend,
    IReadOnlyList<KpiRutaValueDto> ByRuta,
    KpiAppliedGoalDto? Goal,
    string? GoalNote,
    KpiLight Light);

/// <param name="Green">The verde that applies to this ruta in the period, if any.</param>
public record KpiRutaValueDto(int RutaId, string Code, decimal? Value, KpiLight Light, decimal? Green);

/// <param name="Period">Null when the goal is the combination of the rutas' goals.</param>
/// <param name="Scaled">The thresholds were scaled from the goal's own period to the days shown.</param>
/// <param name="FromRutas">There is no goal for the company: these are the goals of the rutas added up (or averaged).</param>
public record KpiAppliedGoalDto(
    KpiGoalPeriod? Period,
    decimal? Red,
    decimal Green,
    decimal? SuperGreen,
    bool Scaled,
    bool FromRutas,
    int? ResponsableUserId,
    string? Responsable,
    DateOnly? ValidFrom);

/// <summary>What forms an indicator in a period: the visits, tickets, pedidos or invitados counted.</summary>
public record KpiDetailDto(
    string Key,
    string Name,
    KpiUnit Unit,
    string Calculation,
    string Label,
    int? RutaId,
    decimal? Value,
    IReadOnlyList<KpiDetailRowDto> Rows);

/// <param name="Numerator">What the row adds to the value (1 when it counts, the prendas, the amount…).</param>
/// <param name="Denominator">What it adds to the total it is divided by (0 when it is only a count).</param>
public record KpiDetailRowDto(
    DateOnly? Date,
    string? RutaCode,
    int? PedidoId,
    int? InvitadoId,
    string? Name,
    string Text,
    decimal Numerator,
    decimal Denominator);

/// <summary>The last weeks of each indicator with its light.</summary>
public record KpiHistoryDto(IReadOnlyList<KpiHistoryWeekDto> Weeks, IReadOnlyList<KpiHistoryRowDto> Indicadores);

public record KpiHistoryWeekDto(DateOnly Start, DateOnly End, string Label);

public record KpiHistoryRowDto(string Key, string Name, KpiArea Area, KpiUnit Unit, IReadOnlyList<KpiHistoryCellDto> Cells);

public record KpiHistoryCellDto(decimal? Value, KpiLight Light);

/// <summary>Metas de los KPIs (WhatsApp y metas › Metas).</summary>
public record KpiGoalsDto(IReadOnlyList<KpiGoalRowDto> Indicadores, IReadOnlyList<KpiResponsableDto> Responsables);

/// <param name="Current">The goal of the whole company in force today.</param>
/// <param name="Next">A goal of the company that starts later, if any.</param>
/// <param name="Rutas">The goals of each active ruta.</param>
/// <param name="History">Every goal it had (company and rutas), newest first.</param>
public record KpiGoalRowDto(
    string Key,
    string Name,
    KpiArea Area,
    KpiUnit Unit,
    bool HigherIsBetter,
    KpiGoalScaling Scaling,
    string Calculation,
    KpiGoalDto? Current,
    KpiGoalDto? Next,
    IReadOnlyList<KpiRutaGoalDto> Rutas,
    IReadOnlyList<KpiGoalDto> History);

public record KpiRutaGoalDto(int RutaId, string Code, KpiGoalDto? Current, KpiGoalDto? Next);

/// <param name="RutaId">Null for the whole company.</param>
public record KpiGoalDto(
    int Id,
    int? RutaId,
    string? RutaCode,
    KpiGoalPeriod Period,
    int? ResponsableUserId,
    string? Responsable,
    decimal? Red,
    decimal Green,
    decimal? SuperGreen,
    DateOnly ValidFrom,
    DateTimeOffset CreatedAt,
    string? CreatedBy);

public record KpiResponsableDto(int Id, string Name, string RoleName);

/// <param name="RutaId">The ruta it is for; null for the whole company.</param>
/// <param name="Red">Optional.</param>
/// <param name="SuperGreen">Optional: without it the indicator is never súper verde.</param>
/// <param name="ValidFrom">From when it applies; today when empty. It cannot be earlier than today.</param>
public record SaveKpiGoalRequest(
    KpiGoalPeriod Period,
    int? ResponsableUserId,
    decimal? Red,
    decimal Green,
    decimal? SuperGreen,
    DateOnly? ValidFrom,
    int? RutaId = null);
