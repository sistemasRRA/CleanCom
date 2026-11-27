using CleanCom.Domain.Common;
using CleanCom.Domain.Crm;

namespace CleanCom.Domain.People;

public enum InvitadoType
{
    /// <summary>Servicio a domicilio.</summary>
    ClienteFinal,
    /// <summary>Comes through an agreement (convenio); counts in the CRM like a cliente final.</summary>
    Convenio,
    Empresa,
    TiendaInterna,
}

/// <summary>
/// CleanCom's client, in the CRM and in the invitado app. CRM status fields arrive with the CRM.
/// </summary>
public class Invitado : Entity, IActivatable
{
    public string Name { get; set; } = "";
    /// <summary>10 digits, normalized with <see cref="Phones.Normalize"/>. Also the app sign-in.</summary>
    public string WhatsAppPhone { get; set; } = "";
    public string? OtherPhone { get; set; }
    public string? Email { get; set; }
    public InvitadoType Type { get; set; } = InvitadoType.ClienteFinal;
    /// <summary>Cómo nos conoció: an option of the catalog (Reason type HowFoundUs), kept as text.</summary>
    public string? HowFoundUs { get; set; }
    /// <summary>
    /// Accepts service notices by WhatsApp (cita agendada, en camino, listo…).
    /// Different from <see cref="DoNotContact"/>, which keeps the invitado out of calls and campaigns.
    /// </summary>
    public bool ReceivesWhatsApp { get; set; } = true;
    /// <summary>Accepts push notices in the app; off, the service notices go by WhatsApp (if accepted).</summary>
    public bool ReceivesPush { get; set; } = true;
    /// <summary>
    /// Accepts promotions by WhatsApp (marketing templates of Meta need it). Off until the invitado accepts; the
    /// campaigns also leave out <see cref="DoNotContact"/>.
    /// </summary>
    public bool ReceivesPromotions { get; set; }
    public string? Campaign { get; set; }
    public string? Notes { get; set; }
    public bool Active { get; set; } = true;
    /// <summary>App account, when the invitado has one.</summary>
    public int? UserId { get; set; }

    // ---- CRM follow-up (Seguimiento). The status itself is calculated, see CrmRules. ----

    /// <summary>Próximo contact: until this date the invitado does not appear in "Llamar hoy".</summary>
    public DateOnly? NextContactOn { get; set; }
    /// <summary>Asked not to be contacted: never in "Llamar hoy" nor in campaigns.</summary>
    public bool DoNotContact { get; set; }
    /// <summary>Status fixed by the office; wins over the calculated one.</summary>
    public CrmStatus? ManualStatus { get; set; }
    /// <summary>Frequency fixed by the office, in days; wins over the calculated one.</summary>
    public int? FixedFrequencyDays { get; set; }
    /// <summary>
    /// Reactivation: sent to "Llamar hoy" from Clientes y fuentes (perdidos). From this date the invitado appears there
    /// whatever their zona and status, until a contact is registered.
    /// </summary>
    public DateOnly? CallRequestedOn { get; set; }
    /// <summary>When the office last sent the invitation to the app (WhatsApp).</summary>
    public DateTimeOffset? InvitedToAppAt { get; set; }

    public List<Address> Addresses { get; set; } = [];
}
