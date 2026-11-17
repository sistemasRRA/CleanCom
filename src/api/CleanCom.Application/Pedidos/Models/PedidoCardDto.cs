using CleanCom.Domain.Pedidos;

namespace CleanCom.Application.Pedidos;

/// <summary>A card of the Pedidos board.</summary>
/// <param name="Folio">The pedido's folio; its format is still to be defined (docs/09-pendientes.md), so it is the id.</param>
/// <param name="DaysInStore">Days since it was collected (the prendas left home); null before that.</param>
/// <param name="DelayText">"Atrasado 6 días · tocaba la entrega el mar 27 oct"; null when on time.</param>
public record PedidoCardDto(
    int Id,
    string Folio,
    int? RutaId,
    string? RutaCode,
    string? RutaColor,
    int InvitadoId,
    string InvitadoName,
    string? Phone,
    string? ZonaCode,
    PedidoEtapa Etapa,
    int? GarmentsApprox,
    IReadOnlyList<string> Lineas,
    DateOnly RecoleccionDate,
    DateOnly? EntregaDate,
    DateTimeOffset? EtapaSince,
    int? DaysInStore,
    DelayLevel DelayLevel,
    string? DelayText,
    int FailedAttempts,
    bool Priority);
