using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;

namespace CleanCom.Domain.Pedidos;

public enum TicketState
{
    EnProceso,
    Listo,
}

/// <summary>Where the ticket came from: captured in Tickets en planta, or received from the POS (pending).</summary>
public enum TicketSource
{
    Manual,
    Pos,
}

/// <summary>
/// A ticket of the POS linked to a pedido: folio, línea de servicio and prendas (docs/03-reglas-de-negocio.md, Planta).
/// The folio is unique among active tickets. Never deleted: removing one is a baja lógica.
/// </summary>
public class Ticket : Entity, IActivatable
{
    public bool Active { get; set; } = true;
    public string Folio { get; set; } = "";
    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }
    public int LineaServicioId { get; set; }
    public LineaServicio? LineaServicio { get; set; }
    public int Garments { get; set; }
    public decimal? Amount { get; set; }
    public TicketState State { get; set; } = TicketState.EnProceso;
    /// <summary>When planta marked it ready (UTC).</summary>
    public DateTimeOffset? ReadyAt { get; set; }
    public TicketSource Source { get; set; } = TicketSource.Manual;

    /// <summary>Folios are compared without spaces and in capitals, so a scanned and a typed folio match.</summary>
    public static string Normalize(string folio) => new string(folio.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
}
