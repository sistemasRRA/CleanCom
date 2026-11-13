using CleanCom.Domain.Time;

namespace CleanCom.Domain.Agenda;

/// <summary>
/// Cutoff rules of docs/03-reglas-de-negocio.md (Agenda). Every instant is evaluated in Mexico City time;
/// the cutoff hours come from settings (Agenda.CorteMismoDia, Agenda.CorteCambioDia, Agenda.CorteCancelacion).
/// </summary>
public static class AgendaCutoffs
{
    /// <summary>
    /// First date a new recolección can take: today when registered before the same-day cutoff, otherwise tomorrow.
    /// Registering exactly at the cutoff no longer makes it the same day.
    /// </summary>
    public static DateOnly MinimumDate(DateTimeOffset registeredAt, TimeOnly corteMismoDia)
    {
        var date = BusinessTime.Date(registeredAt);
        return BusinessTime.TimeOfDay(registeredAt) < corteMismoDia ? date : date.AddDays(1);
    }

    /// <summary>
    /// First date when an invitado moves a recolección from the app: the next day when asked before the
    /// day-change cutoff, two days later from the cutoff on. Example: day 2 at 11:59 → day 3; at 12:01 → day 4.
    /// </summary>
    public static DateOnly MinimumDateForDayChange(DateTimeOffset requestedAt, TimeOnly corteCambioDia)
    {
        var date = BusinessTime.Date(requestedAt);
        return BusinessTime.TimeOfDay(requestedAt) < corteCambioDia ? date.AddDays(1) : date.AddDays(2);
    }

    /// <summary>
    /// Last instant (UTC) an invitado can cancel from the app: the cancellation cutoff of the day before the recolección.
    /// </summary>
    public static DateTimeOffset CancellationDeadline(DateOnly recoleccionDate, TimeOnly corteCancelacion) =>
        BusinessTime.ToUtc(recoleccionDate.AddDays(-1), corteCancelacion);

    /// <summary>
    /// Whether the invitado can still cancel from the app. Up to the deadline included; afterwards only the office.
    /// </summary>
    public static bool CanCancelFromApp(DateTimeOffset now, DateOnly recoleccionDate, TimeOnly corteCancelacion) =>
        now <= CancellationDeadline(recoleccionDate, corteCancelacion);
}
