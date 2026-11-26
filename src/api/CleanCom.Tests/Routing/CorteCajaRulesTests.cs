using CleanCom.Domain.Common;
using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Routing;

/// <summary>Corte de caja (docs/03-reglas-de-negocio.md, Cobro).</summary>
public class CorteCajaRulesTests
{
    private static Dictionary<PaymentMethod, decimal> Amounts(decimal efectivo, decimal tarjeta = 0) =>
        new() { [PaymentMethod.Efectivo] = efectivo, [PaymentMethod.Tarjeta] = tarjeta };

    [Fact]
    public void Every_forma_de_pago_is_compared_and_a_missing_one_counts_as_zero()
    {
        var lines = CorteCajaRules.Lines(Amounts(350, 200), Amounts(350, 200), new Dictionary<PaymentMethod, decimal?>());
        Assert.Equal(Enum.GetValues<PaymentMethod>().Length, lines.Count);
        var transferencia = lines.Single(l => l.Method == PaymentMethod.Transferencia);
        Assert.Equal(0, transferencia.Declared);
        Assert.Null(transferencia.VsPos);
        Assert.DoesNotContain(lines, l => l.HasDifference);
    }

    [Fact]
    public void Missing_money_is_a_negative_difference_against_the_visits_and_the_pos()
    {
        // Example of the document: recorded $350 in cash, declared $330, POS $350 → $20 missing against both.
        var line = new CorteLine(PaymentMethod.Efectivo, Registered: 350, Declared: 330, Pos: 350);
        Assert.Equal(-20, line.VsRegistered);
        Assert.Equal(-20, line.VsPos);
        Assert.True(line.HasDifference);
        // Matching the visits but not the POS is still a difference.
        Assert.True(new CorteLine(PaymentMethod.Tarjeta, 200, 200, 250).HasDifference);
    }

    [Fact]
    public void A_corte_with_differences_needs_a_note_to_close()
    {
        var lines = new[] { new CorteLine(PaymentMethod.Efectivo, 350, 330, null) };
        Assert.Contains("anota qué pasó", Assert.Throws<DomainException>(() => CorteCajaRules.EnsureCanClose(false, lines, " ")).Message);
        CorteCajaRules.EnsureCanClose(false, lines, "Faltaron $20 de cambio; se descuentan.");
        CorteCajaRules.EnsureCanClose(false, [new CorteLine(PaymentMethod.Efectivo, 350, 350, 350)], null);
        Assert.Throws<DomainException>(() => CorteCajaRules.EnsureCanClose(true, [], "x"));
    }

    [Fact]
    public void The_folio_is_the_date_and_the_ruta()
    {
        Assert.Equal("CC-20261126-F", CorteCajaRules.Folio(new DateOnly(2026, 11, 26), "F"));
    }
}
