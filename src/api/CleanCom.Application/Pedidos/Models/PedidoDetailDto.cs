using CleanCom.Domain.Pedidos;

namespace CleanCom.Application.Pedidos;

public record PedidoDetailDto(
    PedidoCardDto Card,
    string Address,
    string? DriverNotes,
    PedidoOrigin Origin,
    DateTimeOffset RegisteredAt,
    IReadOnlyList<PedidoEventoDto> History,
    IReadOnlyList<TicketDto> Tickets,
    IReadOnlyList<PedidoParadaDto> Paradas);
