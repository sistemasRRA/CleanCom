using CleanCom.Domain.People;

namespace CleanCom.Application.Invitados;

/// <param name="Direccion">First address, only when creating.</param>
public record SaveInvitadoRequest(
    string Name,
    string WhatsAppPhone,
    string? OtherPhone,
    string? Email,
    InvitadoType Type,
    string? HowFoundUs,
    string? Campaign,
    string? Notes,
    SaveDireccionRequest? Direccion = null);
