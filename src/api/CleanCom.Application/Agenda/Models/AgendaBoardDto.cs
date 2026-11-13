using CleanCom.Application.Pedidos;

namespace CleanCom.Application.Agenda;

public record BoardRutaDto(int Id, string Code, string Name, string Color);

/// <summary>Paradas that take cupo (fixed ones included) against the ruta's cupo that day.</summary>
public record BoardRutaDayDto(int RutaId, int Occupied, int Cupo);

/// <param name="NonWorking">Reason when the day is a día inhábil; null otherwise.</param>
public record BoardDayDto(DateOnly Date, string Label, string? NonWorking, IReadOnlyList<BoardRutaDayDto> Rutas);

/// <summary>
/// Answer of <c>GET /agenda/tablero</c>: the office agenda by day, with the occupancy per ruta and the scheduled pedidos.
/// </summary>
public record AgendaBoardDto(
    DateOnly From,
    DateOnly To,
    DateOnly Today,
    IReadOnlyList<BoardRutaDto> Rutas,
    IReadOnlyList<BoardDayDto> Days,
    IReadOnlyList<PedidoDto> Pedidos);
