using CleanCom.Domain.Common;

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

    public List<Direccion> Direcciones { get; set; } = [];
}
