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
    IReadOnlyList<DireccionDto> Direcciones);
