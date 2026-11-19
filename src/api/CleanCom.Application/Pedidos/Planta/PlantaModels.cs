using CleanCom.Domain.Pedidos;

namespace CleanCom.Application.Pedidos;

/// <summary>A ticket of the POS (or captured by hand) with its atraso against the processing days of its línea.</summary>
/// <param name="DelayText">"Atrasado 2 días · debía estar listo el lun 16 nov"; null when on time or ready.</param>
public record TicketDto(
    int Id,
    string Folio,
    int PedidoId,
    int LineaServicioId,
    string LineaName,
    int Garments,
    decimal? Amount,
    TicketState State,
    TicketSource Source,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? ReadyAt,
    DelayLevel DelayLevel,
    string? DelayText);

/// <summary>A pedido in Tickets en planta.</summary>
/// <param name="DaysInPlanta">Days since its first ticket arrived.</param>
/// <param name="EntregaDueOrLate">Its entrega is today or already passed: it goes first.</param>
/// <param name="LastDelayNote">The last "¿por qué se atrasa?" of planta, as shown in Pedidos.</param>
public record PlantaCardDto(
    PedidoCardDto Pedido,
    int DaysInPlanta,
    bool EntregaDueOrLate,
    int TicketsReady,
    int TicketsTotal,
    IReadOnlyList<TicketDto> Tickets,
    string? LastDelayNote);

/// <param name="ThresholdDays">"Con N días o más en planta" (Planta.DiasProceso).</param>
public record PlantaIndicatorsDto(
    int TicketsInProcess,
    int PedidosReady,
    int PedidosPartial,
    int EntregaDueOrLate,
    int ManyDaysInPlanta,
    int ThresholdDays);

public enum PlantaView
{
    /// <summary>Pedidos with tickets still in process.</summary>
    EnProceso,
    /// <summary>Pedidos with every ticket ready and not delivered yet.</summary>
    Listos,
}

public record PlantaBoardDto(DateOnly Today, PlantaView View, PlantaIndicatorsDto Indicators, IReadOnlyList<PlantaCardDto> Cards);

/// <summary>
/// Captures a ticket by hand while the POS connection is pending (docs/04-integraciones.md, POS).
/// </summary>
public record CaptureTicketRequest(string Folio, int PedidoId, int LineaServicioId, int Garments, decimal? Amount);

/// <param name="PedidoReady">The ticket was the last one: the pedido went to Listo.</param>
public record TicketReadyResultDto(string Message, bool PedidoReady, PlantaCardDto Card);

/// <summary>"¿Por qué se atrasa? Comentar": motivo of the catalog (atraso en planta) and/or free text.</summary>
public record PlantaCommentRequest(int PedidoId, int? ReasonId, string? Note);

public record DeactivateTicketRequest(string Reason);
