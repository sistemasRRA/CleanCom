using CleanCom.Domain.Avisos;

namespace CleanCom.Application.Avisos;

/// <summary>A notice to an invitado as the office sees it (pedido history, WhatsApp y metas).</summary>
/// <param name="WhatsAppLink">To send it by hand: opens WhatsApp on the number with the text written. Only WhatsApp notices.</param>
/// <param name="Detail">Why it was omitted or failed, or the provider's answer.</param>
public record AvisoDto(
    int Id,
    AvisoEvento Evento,
    string EventoName,
    AvisoCanal? Canal,
    AvisoState State,
    int InvitadoId,
    string InvitadoName,
    string? Phone,
    int? PedidoId,
    string? Title,
    string Text,
    string? Detail,
    string? WhatsAppLink,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt);

/// <summary>Answer of <c>GET /avisos</c>.</summary>
/// <param name="ToSendByHand">WhatsApp notices waiting for the office to send them by hand.</param>
/// <param name="WhatsAppConnected">False while WhatsApp runs in test mode (no Meta account): every WhatsApp notice is sent by hand.</param>
/// <param name="PushConnected">False while push runs in test mode: push notices are only recorded.</param>
public record AvisosDto(IReadOnlyList<AvisoDto> Avisos, int ToSendByHand, bool WhatsAppConnected, bool PushConnected);

/// <param name="Variables">What {{1}}, {{2}}… are, in order.</param>
public record PlantillaDto(
    int Id,
    AvisoEvento Evento,
    string EventoName,
    IReadOnlyList<string> Variables,
    string MetaName,
    string WhatsAppText,
    string PushTitle,
    string PushText,
    bool Active);

public record SavePlantillaRequest(string MetaName, string WhatsAppText, string PushTitle, string PushText, bool Active);

/// <summary>The texts filled in with sample data.</summary>
public record PlantillaPreviewDto(string WhatsApp, string PushTitle, string PushText);

public record AvisoOficinaDto(
    int Id,
    AvisoOficinaKind Kind,
    int InvitadoId,
    string InvitadoName,
    int? PedidoId,
    string Text,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AttendedAt);

/// <summary>Answer of <c>GET /avisos-oficina</c>: the ones not attended and the latest attended.</summary>
public record AvisosOficinaDto(int Pending, IReadOnlyList<AvisoOficinaDto> Avisos);

/// <param name="Token">Expo push token ("ExponentPushToken[…]").</param>
/// <param name="Platform">ios, android or web.</param>
public record RegisterDeviceRequest(string Token, string Platform);

/// <summary>What a sender gets: the notice already filled in, and the device tokens for push.</summary>
public record AvisoMessage(int AvisoId, AvisoCanal Canal, string? Phone, string MetaName, IReadOnlyList<string> Values, string? Title,
    string Text, IReadOnlyList<string> Tokens);

/// <summary>How a send ended: Enviado, Simulado (test mode), ParaMandarAMano or Fallido.</summary>
public record AvisoSendResult(AvisoState State, string? Detail = null);
