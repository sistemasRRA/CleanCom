using CleanCom.Domain.Agenda;

namespace CleanCom.Domain.Fleet;

public enum FleetAlertLevel
{
    /// <summary>The date passed (or the km) and nothing was registered.</summary>
    Vencido,
    /// <summary>Within the notice days.</summary>
    PorVencer,
    /// <summary>Nothing registered yet (no póliza, no service).</summary>
    SinRegistro,
    /// <summary>An open falla or the unidad out of service.</summary>
    Falla,
}

/// <param name="NotifyToday">Today is one of the notice days (or a weekly reminder after the date), so the notice goes out.</param>
public record FleetAlert(FleetAlertEvent Event, FleetAlertLevel Level, string Message, DateOnly? DueDate, bool NotifyToday);

/// <summary>
/// Fleet notices (docs/03-reglas-de-negocio.md, Flota): verificación by the plate's calendar, seguro, tarjeta de
/// circulación, recurring payments and service by km or date, whichever comes first.
/// </summary>
public static class FleetRules
{
    /// <summary>After the date, the reminder repeats every this many days until something is registered.</summary>
    public const int ReminderEveryDays = 7;

    /// <summary>"30,7,0" → 30, 7, 0. Ignores what is not a number of days.</summary>
    public static IReadOnlyList<int> ParseDays(string? text) =>
        (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => int.TryParse(t, out var d) && d >= 0 ? d : -1)
            .Where(d => d >= 0).Distinct().OrderDescending().ToList();

    /// <summary>A notice goes out on each of the days before, and every 7 days once the date passed.</summary>
    public static bool ShouldNotify(int daysLeft, IReadOnlyList<int> daysBefore) =>
        daysBefore.Contains(daysLeft) || (daysLeft < 0 && -daysLeft % ReminderEveryDays == 0);

    /// <summary>
    /// The two months of each semester when the plate verifies in Mexico City, by its last digit: 5-6 in Jan-Feb and
    /// Jul-Aug, 7-8 Feb-Mar and Aug-Sep, 3-4 Mar-Apr and Sep-Oct, 1-2 Apr-May and Oct-Nov, 9-0 May-Jun and Nov-Dec.
    /// Returns the first month of the first semester's window; null when the plate has no digit.
    /// </summary>
    public static int? VerificationStartMonth(string plate)
    {
        var digit = plate.LastOrDefault(char.IsAsciiDigit);
        return digit == default ? null : (digit - '0') switch
        {
            5 or 6 => 1,
            7 or 8 => 2,
            3 or 4 => 3,
            1 or 2 => 4,
            _ => 5,
        };
    }

    /// <summary>
    /// The verificación window (two months) that started most recently on or before <paramref name="today"/>.
    /// </summary>
    public static (DateOnly Start, DateOnly End) CurrentVerificationWindow(int startMonth, DateOnly today)
    {
        var candidates = new[]
        {
            new DateOnly(today.Year, startMonth + 6, 1),
            new DateOnly(today.Year, startMonth, 1),
            new DateOnly(today.Year - 1, startMonth + 6, 1),
        };
        var start = candidates.First(s => s <= today);
        return (start, start.AddMonths(2).AddDays(-1));
    }

    /// <summary>
    /// Verificación: the notice goes out when the window starts, <paramref name="daysBefore"/> before it closes and once
    /// it closed without a verificación registered in it.
    /// </summary>
    public static FleetAlert? Verification(string plate, DateOnly? lastVerifiedOn, DateOnly today, IReadOnlyList<int> daysBefore, bool exempt = false)
    {
        if (exempt)
        {
            // Exenta (for example the trimotos): no calendar and no notice.
            return null;
        }
        if (VerificationStartMonth(plate) is not { } month)
        {
            return new FleetAlert(FleetAlertEvent.Verificacion, FleetAlertLevel.SinRegistro,
                "La placa no tiene número; no se puede calcular su periodo de verificación.", null, false);
        }
        var (start, end) = CurrentVerificationWindow(month, today);
        if (lastVerifiedOn >= start)
        {
            return null;
        }
        var daysLeft = end.DayNumber - today.DayNumber;
        var period = $"{On(start, today)} al {On(end, today)}";
        return daysLeft >= 0
            ? new FleetAlert(FleetAlertEvent.Verificacion, FleetAlertLevel.PorVencer,
                $"Toca verificar: del {period}.", end, today == start || daysBefore.Contains(daysLeft))
            : new FleetAlert(FleetAlertEvent.Verificacion, FleetAlertLevel.Vencido,
                $"Verificación vencida: tocaba del {period}.", end, ShouldNotify(daysLeft, daysBefore));
    }

    /// <summary>Seguro or tarjeta de circulación: by its expiry date.</summary>
    public static FleetAlert? Document(FleetAlertEvent kind, string name, DateOnly? validUntil, DateOnly today, IReadOnlyList<int> daysBefore)
    {
        if (validUntil is not { } until)
        {
            return new FleetAlert(kind, FleetAlertLevel.SinRegistro, $"Sin registro de {name}.", null, false);
        }
        var daysLeft = until.DayNumber - today.DayNumber;
        if (daysLeft < 0)
        {
            return new FleetAlert(kind, FleetAlertLevel.Vencido,
                $"{Capitalize(name)}: venció el {On(until, today)}.", until, ShouldNotify(daysLeft, daysBefore));
        }
        return daysBefore.Count > 0 && daysLeft <= daysBefore[0]
            ? new FleetAlert(kind, FleetAlertLevel.PorVencer,
                $"{Capitalize(name)} vence el {On(until, today)} ({Days(daysLeft)}).", until, daysBefore.Contains(daysLeft))
            : null;
    }

    /// <summary>A recurring payment: by its next date.</summary>
    public static FleetAlert? Payment(string concept, DateOnly nextDate, DateOnly today, IReadOnlyList<int> daysBefore)
    {
        var daysLeft = nextDate.DayNumber - today.DayNumber;
        if (daysLeft < 0)
        {
            return new FleetAlert(FleetAlertEvent.PagoRecurrente, FleetAlertLevel.Vencido,
                $"{concept}: pago vencido desde el {On(nextDate, today)}.", nextDate, ShouldNotify(daysLeft, daysBefore));
        }
        return daysBefore.Count > 0 && daysLeft <= daysBefore[0]
            ? new FleetAlert(FleetAlertEvent.PagoRecurrente, FleetAlertLevel.PorVencer,
                $"{concept}: pago el {On(nextDate, today)} ({Days(daysLeft)}).", nextDate, daysBefore.Contains(daysLeft))
            : null;
    }

    /// <summary>When the next service is due: by date and by km, whichever comes first.</summary>
    public record ServiceDue(DateOnly? Date, int? Km);

    public static ServiceDue? NextService(DateOnly? lastDate, int? lastKm, int everyKm, int everyMonths) =>
        lastDate is null ? null : new ServiceDue(lastDate.Value.AddMonths(everyMonths), lastKm is { } k ? k + everyKm : null);

    /// <summary>Service: due when its date arrives or the km reach it, whatever happens first.</summary>
    public static FleetAlert? Service(ServiceDue? due, int km, DateOnly today, IReadOnlyList<int> daysBefore)
    {
        if (due is null)
        {
            return new FleetAlert(FleetAlertEvent.Servicio, FleetAlertLevel.SinRegistro, "Sin servicio registrado.", null, false);
        }
        if (due.Km is { } dueKm && km >= dueKm)
        {
            // By km there is no "day" to count from: the reminder goes out on Mondays.
            return new FleetAlert(FleetAlertEvent.Servicio, FleetAlertLevel.Vencido,
                $"Servicio vencido por kilometraje: tocaba a los {dueKm:N0} km y lleva {km:N0}.", due.Date, today.DayOfWeek == DayOfWeek.Monday);
        }
        if (due.Date is not { } date)
        {
            return null;
        }
        var daysLeft = date.DayNumber - today.DayNumber;
        if (daysLeft < 0)
        {
            return new FleetAlert(FleetAlertEvent.Servicio, FleetAlertLevel.Vencido,
                $"Servicio vencido desde el {On(date, today)}.", date, ShouldNotify(daysLeft, daysBefore));
        }
        return daysBefore.Count > 0 && daysLeft <= daysBefore[0]
            ? new FleetAlert(FleetAlertEvent.Servicio, FleetAlertLevel.PorVencer,
                $"Servicio el {On(date, today)} ({Days(daysLeft)}).", date, daysBefore.Contains(daysLeft))
            : null;
    }

    /// <summary>The date after a payment, by its frequency.</summary>
    public static DateOnly NextPaymentDate(DateOnly date, PaymentFrequency frequency) => date.AddMonths(frequency switch
    {
        PaymentFrequency.Mensual => 1,
        PaymentFrequency.Bimestral => 2,
        PaymentFrequency.Trimestral => 3,
        PaymentFrequency.Semestral => 6,
        _ => 12,
    });

    /// <summary>The year's spending over the km driven in the year; null while no km were driven.</summary>
    public static decimal? CostPerKm(decimal spentThisYear, int? kmAtStartOfYear, int kmNow) =>
        kmAtStartOfYear is { } start && kmNow > start ? Math.Round(spentThisYear / (kmNow - start), 2) : null;

    /// <summary>"lun 28 jul", with the year when it is not this year ("lun 28 jul 2025").</summary>
    private static string On(DateOnly date, DateOnly today) =>
        date.Year == today.Year ? AgendaCalculator.Format(date) : $"{AgendaCalculator.Format(date)} {date.Year}";

    private static string Days(int days) => days switch { 0 => "hoy", 1 => "mañana", _ => $"en {days} días" };

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
