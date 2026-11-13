using CleanCom.Domain.Common;
using CleanCom.Domain.Crm;

namespace CleanCom.Domain.People;

public enum InvitadoType
{
    ClienteFinal,
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
    /// <summary>Cómo nos conoció.</summary>
    public string? HowFoundUs { get; set; }
    public string? Campaign { get; set; }
    public string? Notes { get; set; }
    public bool Active { get; set; } = true;
    /// <summary>App account, when the invitado has one.</summary>
    public int? UserId { get; set; }

    // ---- CRM follow-up (Seguimiento). The status itself is calculated, see CrmRules. ----

    /// <summary>Próximo contacto: until this date the invitado does not appear in "Llamar hoy".</summary>
    public DateOnly? NextContactOn { get; set; }
    /// <summary>Asked not to be contacted: never in "Llamar hoy" nor in campaigns.</summary>
    public bool DoNotContact { get; set; }
    /// <summary>Status fixed by the office; wins over the calculated one.</summary>
    public CrmStatus? ManualStatus { get; set; }
    /// <summary>Frequency fixed by the office, in days; wins over the calculated one.</summary>
    public int? FixedFrequencyDays { get; set; }

    public List<Direccion> Direcciones { get; set; } = [];
}
