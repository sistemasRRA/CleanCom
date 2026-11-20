using CleanCom.Application.Fleet;
using CleanCom.Domain.Fleet;

namespace CleanCom.Tests.Fleet;

/// <summary>Flota (docs/03-reglas-de-negocio.md): verificación, seguro, tarjeta, pagos y servicio.</summary>
public class FleetRulesTests
{
    private static readonly IReadOnlyList<int> Verificacion = [15, 0];
    private static readonly IReadOnlyList<int> Seguro = [30, 7, 0];
    private static readonly IReadOnlyList<int> Pagos = [15, 3];
    private static readonly IReadOnlyList<int> Servicio = [15, 0];

    private static DateOnly D(int month, int day, int year = 2026) => new(year, month, day);

    [Fact]
    public void Notice_days_are_read_from_the_rule()
    {
        Assert.Equal([30, 7, 0], FleetRules.ParseDays("30, 7,x,0,7"));
        Assert.Empty(FleetRules.ParseDays(null));
    }

    [Theory]
    [InlineData("MXA1235", 1)] // 5-6: Jan-Feb and Jul-Aug
    [InlineData("MXA1238", 2)] // 7-8: Feb-Mar and Aug-Sep
    [InlineData("MXA1234", 3)] // 3-4: Mar-Apr and Sep-Oct
    [InlineData("MXA1232", 4)] // 1-2: Apr-May and Oct-Nov
    [InlineData("MXA1230", 5)] // 9-0: May-Jun and Nov-Dec
    [InlineData("A5B12C3", 3)] // the last digit decides, even with letters after the first ones
    public void The_verification_window_comes_from_the_plates_last_digit(string plate, int startMonth)
    {
        Assert.Equal(startMonth, FleetRules.VerificationStartMonth(plate));
    }

    [Fact]
    public void A_plate_without_digits_cannot_be_placed_in_the_calendar()
    {
        var alert = FleetRules.Verification("ABCDEF", null, D(11, 20), Verificacion);
        Assert.Equal(FleetAlertLevel.SinRegistro, alert!.Level);
    }

    [Fact]
    public void The_current_window_can_be_last_years_second_one()
    {
        // 9 and 0 verify in May-Jun and Nov-Dec: on 1 Mar 2026 the last window was Nov-Dec 2025.
        var (start, end) = FleetRules.CurrentVerificationWindow(5, D(3, 1));
        Assert.Equal(D(11, 1, 2025), start);
        Assert.Equal(D(12, 31, 2025), end);
    }

    [Fact]
    public void Verified_in_the_current_window_has_no_notice()
    {
        // Plate ending in 4: Sep-Oct 2026, verified on 15 Sep.
        Assert.Null(FleetRules.Verification("MXA1234", D(9, 15), D(11, 20), Verificacion));
    }

    [Fact]
    public void Verification_is_due_from_the_start_of_its_window_and_notifies_on_the_first_day_and_15_days_before_closing()
    {
        // Plate ending in 5: Jan-Feb 2026, last verified in Jul 2025.
        var start = FleetRules.Verification("MXA1235", D(7, 10, 2025), D(1, 1), Verificacion)!;
        Assert.Equal(FleetAlertLevel.PorVencer, start.Level);
        Assert.True(start.NotifyToday);
        Assert.Equal(D(2, 28), start.DueDate);

        Assert.True(FleetRules.Verification("MXA1235", D(7, 10, 2025), D(2, 13), Verificacion)!.NotifyToday);
        Assert.False(FleetRules.Verification("MXA1235", D(7, 10, 2025), D(2, 14), Verificacion)!.NotifyToday);
    }

    [Fact]
    public void Verification_not_done_in_its_window_is_overdue_and_reminded_every_7_days()
    {
        // Plate ending in 4: Sep-Oct 2026 closed on 31 Oct without a verificación.
        var alert = FleetRules.Verification("MXA1234", D(3, 10), D(11, 20), Verificacion)!;
        Assert.Equal(FleetAlertLevel.Vencido, alert.Level);
        Assert.False(alert.NotifyToday);
        Assert.True(FleetRules.Verification("MXA1234", D(3, 10), D(11, 7), Verificacion)!.NotifyToday);
    }

    [Fact]
    public void Insurance_notifies_30_and_7_days_before_and_the_day_it_expires()
    {
        var thirty = FleetRules.Document(FleetAlertEvent.Seguro, "seguro", D(12, 20), D(11, 20), Seguro)!;
        Assert.Equal(FleetAlertLevel.PorVencer, thirty.Level);
        Assert.True(thirty.NotifyToday);
        Assert.Contains("en 30 días", thirty.Message);

        Assert.False(FleetRules.Document(FleetAlertEvent.Seguro, "seguro", D(12, 20), D(11, 21), Seguro)!.NotifyToday);
        Assert.True(FleetRules.Document(FleetAlertEvent.Seguro, "seguro", D(12, 20), D(12, 13), Seguro)!.NotifyToday);
        Assert.True(FleetRules.Document(FleetAlertEvent.Seguro, "seguro", D(12, 20), D(12, 20), Seguro)!.NotifyToday);
        // More than 30 days away: nothing yet.
        Assert.Null(FleetRules.Document(FleetAlertEvent.Seguro, "seguro", D(1, 30, 2027), D(11, 20), Seguro));
    }

    [Fact]
    public void Expired_insurance_is_reminded_every_7_days_and_a_missing_one_is_flagged()
    {
        var expired = FleetRules.Document(FleetAlertEvent.Seguro, "seguro", D(11, 13), D(11, 20), Seguro)!;
        Assert.Equal(FleetAlertLevel.Vencido, expired.Level);
        Assert.True(expired.NotifyToday);
        Assert.Equal("Seguro vencido desde el vie 13 nov.", expired.Message);

        Assert.Equal(FleetAlertLevel.SinRegistro, FleetRules.Document(FleetAlertEvent.Seguro, "seguro", null, D(11, 20), Seguro)!.Level);
    }

    [Fact]
    public void Recurring_payments_notify_15_and_3_days_before_and_move_by_their_frequency()
    {
        var alert = FleetRules.Payment("Tenencia", D(12, 5), D(11, 20), Pagos)!;
        Assert.Equal(FleetAlertLevel.PorVencer, alert.Level);
        Assert.True(alert.NotifyToday);
        Assert.Equal(D(2, 5, 2027), FleetRules.NextPaymentDate(D(12, 5), PaymentFrequency.Bimestral));
        Assert.Equal(D(12, 5, 2027), FleetRules.NextPaymentDate(D(12, 5), PaymentFrequency.Anual));
    }

    [Fact]
    public void Service_is_due_by_km_or_date_whichever_comes_first()
    {
        // Last service on 1 May at 20,000 km; every 10,000 km or 6 months → 1 Nov or 30,000 km.
        var due = FleetRules.NextService(D(5, 1), 20_000, 10_000, 6)!;
        Assert.Equal(D(11, 1), due.Date);
        Assert.Equal(30_000, due.Km);

        Assert.Equal(FleetAlertLevel.PorVencer, FleetRules.Service(due, 25_000, D(10, 20), Servicio)!.Level);
        var byKm = FleetRules.Service(due, 30_500, D(10, 20), Servicio)!;
        Assert.Equal(FleetAlertLevel.Vencido, byKm.Level);
        Assert.Contains("kilometraje", byKm.Message);
        var byDate = FleetRules.Service(due, 26_000, D(11, 2), Servicio)!;
        Assert.Equal(FleetAlertLevel.Vencido, byDate.Level);
        Assert.Contains("desde el", byDate.Message);
        Assert.Null(FleetRules.Service(due, 21_000, D(6, 1), Servicio));
    }

    [Fact]
    public void A_unidad_without_a_service_is_flagged()
    {
        Assert.Null(FleetRules.NextService(null, null, 10_000, 6));
        Assert.Equal(FleetAlertLevel.SinRegistro, FleetRules.Service(null, 0, D(11, 20), Servicio)!.Level);
    }

    [Fact]
    public void Cost_per_km_is_the_years_spending_over_the_years_km()
    {
        Assert.Equal(1.00m, FleetRules.CostPerKm(5_000m, 20_000, 25_000));
        Assert.Null(FleetRules.CostPerKm(5_000m, null, 25_000));
        Assert.Null(FleetRules.CostPerKm(5_000m, 25_000, 25_000));
    }

    [Fact]
    public void Plates_are_stored_without_spaces_or_dashes_in_capitals()
    {
        Assert.Equal("ABC1234", UnidadService.NormalizePlate("abc-123 4"));
    }
}
