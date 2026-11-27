using CleanCom.Domain.People;

namespace CleanCom.Application.Invitados;

/// <param name="HowFoundUs">An option of the "Cómo nos conoció" catalog (or the value the invitado already had).</param>
/// <param name="ReceivesWhatsApp">Accepts service notices by WhatsApp; true when not sent.</param>
/// <param name="Address">First address, only when creating.</param>
/// <param name="ReceivesPush">Accepts push notices; null keeps what it had (true for a new invitado).</param>
/// <param name="ReceivesPromotions">Accepts promotions by WhatsApp; null keeps what it had (false for a new invitado).</param>
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
    bool ReceivesWhatsApp = true,
    bool? ReceivesPush = null,
    bool? ReceivesPromotions = null);
