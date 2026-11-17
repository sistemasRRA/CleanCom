using CleanCom.Domain.Routing;

namespace CleanCom.Application.Pedidos;

/// <summary>A visit of the pedido (recolección or entrega), including attempts that were not done.</summary>
public record PedidoParadaDto(int Id, ParadaKind Kind, DateOnly Date, string Label, string RutaCode, ParadaState State, DateTimeOffset? ArrivedAt, bool Active);
