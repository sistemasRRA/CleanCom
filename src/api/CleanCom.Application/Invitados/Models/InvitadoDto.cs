using CleanCom.Domain.People;

namespace CleanCom.Application.Invitados;

public record InvitadoDto(
    int Id,
    string Name,
    string WhatsAppPhone,
    string? OtherPhone,
    string? Email,
    InvitadoType Type,
    string? HowFoundUs,
    string? Campaign,
    string? Notes,
    bool Active,
    bool ReceivesWhatsApp,
    IReadOnlyList<AddressDto> Addresses);

/// <summary>An active invitado who already has a phone being captured.</summary>
/// <param name="BlocksSave">Same WhatsApp: the capture cannot be saved; otherwise only a warning.</param>
public record PhoneMatchDto(int Id, string Name, string WhatsAppPhone, string? OtherPhone, bool BlocksSave);
