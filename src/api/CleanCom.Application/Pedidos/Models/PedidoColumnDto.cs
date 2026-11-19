using CleanCom.Domain.Pedidos;

namespace CleanCom.Application.Pedidos;

public record PedidoColumnDto(PedidoEtapa Etapa, string Name, int Count, IReadOnlyList<PedidoCardDto> Cards);
