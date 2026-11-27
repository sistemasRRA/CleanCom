namespace CleanCom.Application.Invitados;

/// <summary>"Mis datos" of the invitado app: what the invitado sees of their own record (no office notes).</summary>
/// <param name="ProfileComplete">The invitado gave their name and "¿cómo nos conociste?" (asked once).</param>
public record MeDto(
    int Id,
    string Name,
    string WhatsAppPhone,
    string? OtherPhone,
    string? Email,
    string? HowFoundUs,
    bool ReceivesWhatsApp,
    bool ReceivesPush,
    bool ReceivesPromotions,
    bool ProfileComplete,
    IReadOnlyList<AddressDto> Addresses);

/// <summary>The cell phone is the sign-in and cannot be changed from the app.</summary>
/// <param name="HowFoundUs">An option of the "Cómo nos conoció" catalog (GET /motivos?type=HowFoundUs).</param>
/// <param name="ReceivesPush">Push notices; null keeps what it had (versions of the app before this field).</param>
/// <param name="ReceivesPromotions">Promotions by WhatsApp; null keeps what it had.</param>
public record UpdateMeRequest(
    string Name,
    string? Email,
    string? OtherPhone,
    string? HowFoundUs,
    bool ReceivesWhatsApp = true,
    bool? ReceivesPush = null,
    bool? ReceivesPromotions = null);
