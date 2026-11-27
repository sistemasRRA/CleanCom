using CleanCom.Domain.Routing;

namespace CleanCom.Application.Evidencias;

/// <summary>Filters of the visits (Evidencias y caja). Dates in Mexico City; today when empty.</summary>
/// <param name="Desde">First day (yyyy-MM-dd).</param>
/// <param name="Hasta">Last day, included; at most 92 days after Desde.</param>
/// <param name="FormaPago">Only visits with a reference payment of this forma.</param>
public record VisitasQuery(DateOnly? Desde, DateOnly? Hasta, int? RutaId, PaymentMethod? FormaPago, int Pagina = 1, int PorPagina = 50);

/// <summary>A photo of a visit; <c>OutsideRadius</c> when taken farther than Gps.ToleranciaMetros from the parada.</summary>
public record VisitaPhotoDto(int Id, DateTimeOffset TakenAt, int? DistanceMeters, bool OutsideRadius);

/// <summary>A closed visit (done or not done) with its evidence and reference payment.</summary>
/// <param name="Name">The invitado, or the name of the parada fija or lugar.</param>
/// <param name="Tickets">Folios of the pedido's tickets.</param>
/// <param name="SignatureId">The invitado's signature (entregas), opened with GET /evidencias/{id}/archivo.</param>
/// <param name="Reason">Why it was not done (catalog motivo and/or what the chofer wrote).</param>
public record VisitaDto(
    int ParadaId,
    DateOnly Date,
    string RutaCode,
    string RutaColor,
    string? Chofer,
    ParadaKind Kind,
    ParadaState State,
    string Name,
    int? PedidoId,
    IReadOnlyList<string> Tickets,
    DateTimeOffset? ClosedAt,
    int? Garments,
    PaymentMethod? PaymentMethod,
    decimal? PaymentAmount,
    IReadOnlyList<VisitaPhotoDto> Photos,
    int? SignatureId,
    string? Notes,
    string? Reason,
    bool ArrivedOutsideRadius);

/// <param name="Done">Visits done in the filters (all pages).</param>
/// <param name="NotDone">Visits not done in the filters.</param>
/// <param name="Collected">Reference payments of the visits in the filters.</param>
/// <param name="ByMethod">Collected per forma de pago.</param>
/// <param name="OutsideRadius">Visits with a photo or arrival farther than Gps.ToleranciaMetros.</param>
public record VisitasTotalsDto(int Done, int NotDone, decimal Collected, IReadOnlyList<MethodTotalDto> ByMethod, int OutsideRadius);

public record MethodTotalDto(PaymentMethod Method, decimal Amount, int Count);

/// <summary>Answer of <c>GET /visitas</c>.</summary>
/// <param name="ToleranceMeters">Gps.ToleranciaMetros: a photo farther than this is marked.</param>
public record VisitasDto(
    DateOnly Desde,
    DateOnly Hasta,
    IReadOnlyList<VisitaDto> Rows,
    int RowCount,
    int Page,
    int PageSize,
    VisitasTotalsDto Totals,
    int ToleranceMeters);

/// <summary>A visit of a pedido with its evidence (galería del pedido).</summary>
public record PedidoVisitaEvidenceDto(
    int ParadaId,
    DateOnly Date,
    ParadaKind Kind,
    ParadaState State,
    string RutaCode,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<VisitaPhotoDto> Photos,
    int? SignatureId);

/// <summary>Answer of <c>GET /pedidos/{id}/evidencias</c>: the photos of recolección and entrega of a pedido.</summary>
public record PedidoEvidenceDto(int PedidoId, string Invitado, IReadOnlyList<PedidoVisitaEvidenceDto> Visits);

public enum CorteState
{
    /// <summary>The chofer has not started the shift.</summary>
    SinTurno,
    /// <summary>Shift started, not closed: nothing declared yet.</summary>
    TurnoAbierto,
    /// <summary>The chofer declared; the office has to review and close it.</summary>
    PorRevisar,
    Cerrado,
}

/// <param name="Registered">Recorded in the visits (the snapshot once closed).</param>
/// <param name="Declared">Declared by the chofer; null before the shift is closed.</param>
/// <param name="Pos">Captured by the office when closing; null when not compared.</param>
/// <param name="VsRegistered">Declared minus recorded: negative when money is missing.</param>
/// <param name="VsPos">Declared minus POS.</param>
public record CorteLineDto(PaymentMethod Method, decimal Registered, int RegisteredCount, decimal? Declared, decimal? Pos, decimal? VsRegistered, decimal? VsPos);

/// <summary>The corte de caja of one ruta on a day.</summary>
/// <param name="CorteId">Null until the shift is closed (nothing to close yet).</param>
/// <param name="ShiftClosedBy">The office user who closed the shift for the chofer; null when the chofer did.</param>
/// <param name="OpenParadas">Paradas not closed yet (to know before closing the shift from the office).</param>
/// <param name="ReopenedAt">The last time the corte was reopened, by whom and why.</param>
public record CorteDto(
    int RutaDiaId,
    int? CorteId,
    string RutaCode,
    string RutaColor,
    string? Chofer,
    string? Unidad,
    DateTimeOffset? ShiftStartedAt,
    DateTimeOffset? ShiftEndedAt,
    int? StartKm,
    int? EndKm,
    CorteState State,
    IReadOnlyList<CorteLineDto> Lines,
    decimal Registered,
    decimal? Declared,
    bool HasDifference,
    string? ChoferNotes,
    string? Folio,
    DateTimeOffset? ClosedAt,
    string? ClosedBy,
    string? OfficeNotes,
    string? ShiftClosedBy,
    int OpenParadas,
    DateTimeOffset? ReopenedAt,
    string? ReopenedBy,
    string? ReopenReason);

/// <summary>Answer of <c>GET /cortes</c>.</summary>
/// <param name="PorRevisar">Cortes declared by the chofer and not closed yet, that day.</param>
public record CortesDto(DateOnly Date, string Label, IReadOnlyList<CorteDto> Cortes, int PorRevisar);

/// <summary>What the POS says for one forma de pago; null amount = not compared.</summary>
public record PosAmountRequest(PaymentMethod Method, decimal? Amount);

/// <summary>The office closes a corte: POS amounts (until the POS is connected) and notes, required with differences.</summary>
public record CloseCorteRequest(IReadOnlyList<PosAmountRequest>? Pos, string? Notes);

/// <summary>Reopening a closed corte (permiso reabrir-cortes): why.</summary>
public record ReopenCorteRequest(string? Reason);

/// <summary>
/// The office closes a chofer's shift (permiso cerrar-turnos): what the chofer handed in per forma de pago, the final
/// km when known, and why. <c>Confirm</c> answers the API's question (long shift, paradas still open).
/// </summary>
public record OfficeEndShiftRequest(
    int? Km,
    IReadOnlyList<CleanCom.Application.Chofer.DeclaredPaymentRequest> Declared,
    string? Notes,
    bool Confirm = false);
