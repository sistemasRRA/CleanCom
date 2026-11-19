namespace CleanCom.Application.Pedidos;

/// <summary>Answer of <c>GET /pedidos/tablero</c>. Entregado shows the last 7 days; Cancelado is not on the board.</summary>
/// <param name="LateCount">Late or with failed attempts, for the "Atrasados e intentos fallidos" filter.</param>
public record PedidoBoardDto(DateOnly Today, int LateCount, IReadOnlyList<PedidoColumnDto> Columns);
