using CleanCom.Domain.Common;

namespace CleanCom.Domain.Kpis;

/// <summary>
/// Meta de un indicador (MetaIndicador): rojo, verde y súper verde, the period they are written for and who answers for
/// it, from <see cref="ValidFrom"/> on, for the whole company or for one ruta. A change is a new row, so past periods keep
/// the goal they had. Never deleted.
/// </summary>
public class KpiGoal : Entity, IActivatable
{
    public bool Active { get; set; } = true;
    /// <summary><see cref="KpiDefinition.Key"/>.</summary>
    public string IndicadorKey { get; set; } = "";
    public KpiGoalPeriod Period { get; set; }
    /// <summary>The ruta it is for; null for the whole company.</summary>
    public int? RutaId { get; set; }
    /// <summary>The office user who answers for the indicator.</summary>
    public int? ResponsableUserId { get; set; }
    /// <summary>Optional: shown as reference (below verde is rojo anyway).</summary>
    public decimal? Red { get; set; }
    public decimal Green { get; set; }
    /// <summary>Optional: without it the indicator is never súper verde.</summary>
    public decimal? SuperGreen { get; set; }
    /// <summary>Business date from which it applies.</summary>
    public DateOnly ValidFrom { get; set; }
}

/// <summary>
/// Valor de un indicador (ValorIndicador) for a period, per ruta (null = all), saved by the nightly job so the screen
/// does not recalculate everything on each visit. Recalculating updates the row in place.
/// </summary>
public class KpiValue : Entity
{
    public string IndicadorKey { get; set; } = "";
    public KpiPeriod Period { get; set; }
    public DateOnly PeriodStart { get; set; }
    public int? RutaId { get; set; }
    /// <summary>Null when there was nothing to measure.</summary>
    public decimal? Value { get; set; }
    public DateTimeOffset CalculatedAt { get; set; }
}
