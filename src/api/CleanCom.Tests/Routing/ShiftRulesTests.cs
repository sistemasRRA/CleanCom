using CleanCom.Domain.Common;
using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Routing;

/// <summary>Inicio and fin de turno: odometer readings (docs/03-reglas-de-negocio.md, Turno del chofer; Unidad.KmMaximoPorTurno).</summary>
public class ShiftRulesTests
{
    private const int Max = 400;

    [Fact]
    public void The_starting_km_cannot_be_lower_than_the_unidad()
    {
        // Example of the document: the unidad is at 45,200 km; 45,150 is rejected.
        var error = Assert.Throws<DomainException>(() => ShiftRules.CheckStart(45_150, 45_200, "NY2221B", Max));
        Assert.Contains("45,200 km", error.Message);
        Assert.Contains("NY2221B", error.Message);
        Assert.Null(ShiftRules.CheckStart(45_200, 45_200, "NY2221B", Max));
        Assert.Null(ShiftRules.CheckStart(45_260, 45_200, "NY2221B", Max));
    }

    [Fact]
    public void A_jump_bigger_than_the_max_per_shift_asks_to_confirm()
    {
        // 45,200 → 452,000 (one digit more) asks; up to 400 km more does not.
        Assert.Contains("406,800 km más", ShiftRules.CheckStart(452_000, 45_200, "NY2221B", Max));
        Assert.Null(ShiftRules.CheckStart(45_600, 45_200, "NY2221B", Max));
        Assert.NotNull(ShiftRules.CheckStart(45_601, 45_200, "NY2221B", Max));
    }

    [Fact]
    public void A_unidad_without_km_accepts_any_starting_reading()
    {
        Assert.Null(ShiftRules.CheckStart(120_000, 0, "7J5BR", Max));
        Assert.Throws<DomainException>(() => ShiftRules.CheckStart(-1, 0, "7J5BR", Max));
    }

    [Fact]
    public void The_final_km_cannot_be_lower_than_the_starting_one()
    {
        var error = Assert.Throws<DomainException>(() => ShiftRules.CheckEnd(45_190, 45_200, Max, 0));
        Assert.Contains("45,200 km", error.Message);
        Assert.Empty(ShiftRules.CheckEnd(45_200, 45_200, Max, 0));
        // Example of the document: 45,200 → 45,268 is a normal day.
        Assert.Empty(ShiftRules.CheckEnd(45_268, 45_200, Max, 0));
    }

    [Fact]
    public void Todays_paradas_are_worked_only_during_the_shift()
    {
        var start = DateTimeOffset.Parse("2026-11-26T14:05:00Z");
        var end = DateTimeOffset.Parse("2026-11-27T00:10:00Z");
        Assert.Contains("Inicia tu turno", Assert.Throws<DomainException>(() => ShiftRules.EnsureOpen(null, null, isToday: true)).Message);
        ShiftRules.EnsureOpen(start, null, isToday: true);
        Assert.Contains("Ya cerraste tu turno", Assert.Throws<DomainException>(() => ShiftRules.EnsureOpen(start, end, isToday: true)).Message);
        // An action of an earlier day that syncs late (sin señal) is accepted.
        ShiftRules.EnsureOpen(null, null, isToday: false);
    }

    [Fact]
    public void A_long_shift_or_open_paradas_ask_to_confirm_together()
    {
        var warnings = ShiftRules.CheckEnd(46_000, 45_200, Max, 2);
        Assert.Equal(2, warnings.Count);
        Assert.Contains("Recorriste 800 km", warnings[0]);
        Assert.Contains("Te quedan 2 paradas sin cerrar", warnings[1]);
        Assert.Equal("Te queda 1 parada sin cerrar; oficina la verá.", Assert.Single(ShiftRules.CheckEnd(45_268, 45_200, Max, 1)));
    }
}
