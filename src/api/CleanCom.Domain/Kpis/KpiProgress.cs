namespace CleanCom.Domain.Kpis;

/// <summary>
/// Where a value stands against its goal, to draw it as a bar: positions from 0 (bad end) to 100 (good end), whatever way
/// is better, and how much is missing for the next color.
/// </summary>
/// <param name="Next">The color it can reach next (Verde, then SuperVerde); null when it has the best one there is.</param>
/// <param name="Gap">How much the value has to move to reach <paramref name="Next"/> (always positive).</param>
public record KpiProgress(
    decimal ValuePosition,
    decimal? RedPosition,
    decimal GreenPosition,
    decimal? SuperGreenPosition,
    KpiLight? Next,
    decimal? Gap);

public static class KpiProgressRules
{
    /// <summary>
    /// The bar goes from the lowest to the highest of rojo, verde, súper verde and the value, with a margin of 10 % of
    /// that span on each side, and is turned around when less is better so that the right is always the good side.
    /// Example (more is better): rojo 8, verde 10, súper verde 11, value 9 → span 7.7 to 11.3; faltan 1 para verde.
    /// Null without value or goal.
    /// </summary>
    public static KpiProgress? Of(decimal? value, KpiThresholds? goal, bool higherIsBetter)
    {
        if (value is not { } v || goal is not { } g)
        {
            return null;
        }
        decimal[] points = [v, g.Green, .. new[] { g.Red, g.SuperGreen }.OfType<decimal>()];
        var low = points.Min();
        var high = points.Max();
        var margin = high == low ? Math.Max(1, Math.Abs(high) * 0.1m) : (high - low) * 0.1m;
        low -= margin;
        high += margin;
        decimal Position(decimal x)
        {
            var p = (x - low) / (high - low) * 100;
            return Math.Round(higherIsBetter ? p : 100 - p, 1, MidpointRounding.AwayFromZero);
        }
        decimal? Optional(decimal? x) => x is { } d ? Position(d) : null;

        bool Reaches(decimal threshold) => higherIsBetter ? v >= threshold : v <= threshold;
        decimal Distance(decimal threshold) => Math.Round(Math.Abs(threshold - v), 2, MidpointRounding.AwayFromZero);
        var (next, gap) = !Reaches(g.Green) ? (KpiLight.Verde, Distance(g.Green))
            : g.SuperGreen is { } super && !Reaches(super) ? (KpiLight.SuperVerde, Distance(super))
            : ((KpiLight?)null, (decimal?)null);
        return new KpiProgress(Position(v), Optional(g.Red), Position(g.Green), Optional(g.SuperGreen), next, gap);
    }
}
