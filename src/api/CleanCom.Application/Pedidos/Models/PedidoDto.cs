using CleanCom.Domain.Pedidos;

namespace CleanCom.Application.Pedidos;

/// <param name="Address">Street, number and colonia in one line, as the office reads it.</param>
/// <param name="RutaCode">Ruta of its recolección parada.</param>
public record PedidoDto(
    int Id,
    int InvitadoId,
    string InvitadoName,
    string WhatsAppPhone,
    int DireccionId,
    string Address,
    string? ZonaCode,
    string? RutaCode,
    PedidoEtapa Etapa,
    Turno Turno,
    TimeOnly? SpecificTime,
    bool Priority,
    int? GarmentsApprox,
    string? DriverNotes,
    DateTimeOffset RegisteredAt,
    PedidoOrigin Origin,
    DateOnly RecoleccionDate,
    IReadOnlyList<string> LineasServicio);
