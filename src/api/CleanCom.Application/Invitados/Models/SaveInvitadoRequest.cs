using CleanCom.Domain.People;

namespace CleanCom.Application.Invitados;

/// <param name="HowFoundUs">An option of the "Cómo nos conoció" catalog (or the value the invitado already had).</param>
/// <param name="ReceivesWhatsApp">Accepts notices by WhatsApp; true when not sent.</param>
/// <param name="Address">First address, only when creating.</param>
public record SaveInvitadoRequest(
    string Name,
    string WhatsAppPhone,
    string? OtherPhone,
    string? Email,
    InvitadoType Type,
    string? HowFoundUs,
    string? Campaign,
    string? Notes,
    SaveAddressRequest? Address = null,
    bool ReceivesWhatsApp = true);
