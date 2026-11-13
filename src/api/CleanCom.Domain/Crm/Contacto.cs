using CleanCom.Domain.Common;
using CleanCom.Domain.People;

namespace CleanCom.Domain.Crm;

public enum ContactChannel
{
    Llamada,
    WhatsApp,
}

/// <summary>Result of a CRM contact; it decides when to contact again when the office leaves the date empty.</summary>
public enum ContactResult
{
    NoContesto,
    LlamarDespues,
    NoPorAhora,
    Agendo,
    /// <summary>Asked not to be contacted again: they leave "Llamar hoy" and campaigns.</summary>
    NoContactar,
}

/// <summary>Days to wait per result (Crm.DiasNoContesto, Crm.DiasLlamarDespues, Crm.DiasNoPorAhora).</summary>
public record FollowUpDays(int NoAnswer, int CallLater, int NotNow);

/// <summary>
/// A call or WhatsApp to an invitado from the CRM (docs/03-reglas-de-negocio.md, CRM › Registro de contacto).
/// </summary>
public class Contacto : Entity, IActivatable
{
    public int InvitadoId { get; set; }
    public Invitado? Invitado { get; set; }
    public ContactChannel Channel { get; set; }
    public ContactResult Result { get; set; }
    public string? Note { get; set; }
    /// <summary>When to contact again; computed from the result when the office leaves it empty.</summary>
    public DateOnly? NextContactOn { get; set; }
    /// <summary>When it happened (UTC).</summary>
    public DateTimeOffset ContactedAt { get; set; }
    public bool Active { get; set; } = true;

    /// <summary>
    /// Date to contact again when none was given: tomorrow if they did not answer, in 3 days if they asked to be called
    /// later, in 3 weeks if they said not for now (days from settings). Nothing after scheduling or when they asked not
    /// to be contacted.
    /// </summary>
    public static DateOnly? DefaultNextContact(ContactResult result, DateOnly today, FollowUpDays days) => result switch
    {
        ContactResult.NoContesto => today.AddDays(days.NoAnswer),
        ContactResult.LlamarDespues => today.AddDays(days.CallLater),
        ContactResult.NoPorAhora => today.AddDays(days.NotNow),
        _ => null,
    };
}
