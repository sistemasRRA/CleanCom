using CleanCom.Domain.Pedidos;

namespace CleanCom.Application.Pedidos;

/// <param name="By">Who did it; null when the system did (chofer app, planta, POS) or unknown.</param>
/// <param name="Text">The line as the office reads it, in Spanish.</param>
public record PedidoEventoDto(
    int Id,
    DateTimeOffset At,
    string? By,
    PedidoEventoKind Kind,
    PedidoEtapa? FromEtapa,
    PedidoEtapa? ToEtapa,
    DateOnly? Date,
    bool Automatic,
    string? Reason,
    string? Note,
    string Text);
