using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.Pedidos;

/// <summary>A visit of the pedido (recolección or entrega), including attempts that were not done.</summary>
/// <param name="ClosedAt">When the chofer closed it, done or not.</param>
/// <param name="CloseNote">Chofer's notes, or the motivo when it was not done.</param>
/// <param name="Evidence">Photos and signature; the file is at <c>GET /evidencias/{id}/archivo</c>.</param>
/// <param name="Turno">Mañana, tarde, cualquier hora or hora específica of this visit.</param>
public record PedidoParadaDto(
    int Id,
    ParadaKind Kind,
    DateOnly Date,
    string Label,
    string RutaCode,
    ParadaState State,
    DateTimeOffset? ArrivedAt,
    bool Active,
    DateTimeOffset? ClosedAt = null,
    int? GarmentsCollected = null,
    string? CloseNote = null,
    PaymentMethod? PaymentMethod = null,
    decimal? PaymentAmount = null,
    IReadOnlyList<EvidenceRefDto>? Evidence = null,
    Turno? Turno = null);

public record EvidenceRefDto(int Id, EvidenceKind Kind, DateTimeOffset TakenAt, int? DistanceMeters);
