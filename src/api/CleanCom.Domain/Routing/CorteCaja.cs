using CleanCom.Domain.Common;

namespace CleanCom.Domain.Routing;

/// <summary>
/// Corte de caja of a chofer's shift (docs/03-reglas-de-negocio.md, Cobro): what the chofer declares at the end of the
/// shift, per forma de pago. The office compares it against the reference payments of the visits and the POS, writes
/// down the differences and closes it with a folio (Evidencias y caja). One per RutaDia. Never deleted.
/// </summary>
public class CorteCaja : Entity
{
    public int RutaDiaId { get; set; }
    public RutaDia? RutaDia { get; set; }
    public int? OperadorId { get; set; }
    public DateOnly Date { get; set; }
    /// <summary>What the chofer wrote when declaring (for example, why some money is missing).</summary>
    public string? ChoferNotes { get; set; }

    /// <summary>"CC-20261126-F", given when the office closes it.</summary>
    public string? Folio { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    /// <summary>User of the office who closed it (responsable).</summary>
    public int? ClosedBy { get; set; }
    /// <summary>What the office wrote about the differences.</summary>
    public string? OfficeNotes { get; set; }

    public List<CorteCajaLine> Lines { get; set; } = [];
}

/// <summary>One forma de pago in a corte de caja.</summary>
public class CorteCajaLine : Entity
{
    public int CorteCajaId { get; set; }
    public PaymentMethod Method { get; set; }
    /// <summary>What the chofer declared.</summary>
    public decimal Declared { get; set; }
    /// <summary>What the visits recorded when the office closed the corte (it no longer changes after that).</summary>
    public decimal? Registered { get; set; }
    /// <summary>What the POS says, captured by the office when closing until the POS is connected; null when not compared.</summary>
    public decimal? Pos { get; set; }
}
