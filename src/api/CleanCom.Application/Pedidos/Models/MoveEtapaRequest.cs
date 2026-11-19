using CleanCom.Domain.Pedidos;

namespace CleanCom.Application.Pedidos;

/// <param name="Reason">Required to go back, to deliver or cancel by hand, or to leave Cancelado.</param>
public record MoveEtapaRequest(PedidoEtapa Etapa, string? Reason);
