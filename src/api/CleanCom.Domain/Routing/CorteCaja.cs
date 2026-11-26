using CleanCom.Domain.Common;

namespace CleanCom.Domain.Routing;

/// <summary>
/// Corte de caja of a chofer's shift (docs/03-reglas-de-negocio.md, Cobro): what the chofer declares at the end of the
/// shift, per forma de pago. The office compares it against the reference payments of the visits and the POS
/// (Evidencias y caja). One per RutaDia. Never deleted.
/// </summary>
public class CorteCaja : Entity
{
    public int RutaDiaId { get; set; }
    public RutaDia? RutaDia { get; set; }
    public int? OperadorId { get; set; }
    public DateOnly Date { get; set; }
    /// <summary>What the chofer wrote when declaring (for example, why some money is missing).</summary>
    public string? ChoferNotes { get; set; }

    public List<CorteCajaLine> Lines { get; set; } = [];
}

/// <summary>Declared amount of one forma de pago in a corte de caja.</summary>
public class CorteCajaLine : Entity
{
    public int CorteCajaId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Declared { get; set; }
}
