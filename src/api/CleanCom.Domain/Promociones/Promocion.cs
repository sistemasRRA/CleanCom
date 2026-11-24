using CleanCom.Domain.Common;
using CleanCom.Domain.Routing;

namespace CleanCom.Domain.Promociones;

/// <summary>Programs of docs/03-reglas-de-negocio.md (Promociones).</summary>
public enum PromocionPrograma
{
    /// <summary>Sellos de Promociones Cruzadas, for recurring invitados.</summary>
    Sellos,
    /// <summary>Cuponera de Bienvenida, for new invitados when their first service closes.</summary>
    Cuponera,
    /// <summary>Sent to a CRM segment (arrives with notifications).</summary>
    Campana,
}

public enum DiscountType
{
    Porcentaje,
    Monto,
    /// <summary>An item for free ("Carga extra gratis"); Value is not used.</summary>
    ArticuloGratis,
    /// <summary>A special price in pesos ("Sneaker sencillo a $100"); Value is the price.</summary>
    PrecioEspecial,
}

/// <summary>
/// A promotion with structured rules (docs/pantallas/oficina/promociones.md). The discount itself is applied in the POS;
/// the system grants, reserves and marks the beneficios.
/// </summary>
public class Promocion : Entity, IActivatable
{
    public PromocionPrograma Programa { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Línea the discount applies to; null = any.</summary>
    public int? LineaServicioId { get; set; }
    public DiscountType DiscountType { get; set; }
    /// <summary>Percentage (20 = 20%) or amount in pesos; 0 for an item for free.</summary>
    public decimal Value { get; set; }
    /// <summary>Minimum ticket amount in pesos for the discount.</summary>
    public decimal? MinimumAmount { get; set; }
    /// <summary>Only with this payment method; null = any.</summary>
    public PaymentMethod? PaymentMethod { get; set; }
    /// <summary>Can be combined with another beneficio on the same pedido.</summary>
    public bool Stackable { get; set; }
    /// <summary>
    /// Sellos: granted when a recolección of a pedido with this línea closes (the invitado's second service onwards);
    /// null = any service. Not used by other programs.
    /// </summary>
    public int? TriggerLineaServicioId { get; set; }
    /// <summary>
    /// The mechanic as the invitado reads it ("20% de descuento en el lavado de un edredón…"); when empty it is written
    /// from the structured rules.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>Free text with the conditions, shown to the invitado and the chofer.</summary>
    public string? Condition { get; set; }
    /// <summary>
    /// Days a granted beneficio is valid (60 for the cuponera). Null (sellos only) = until the entrega of the pedido that
    /// gave it.
    /// </summary>
    public int? ValidityDays { get; set; }
    public bool Active { get; set; } = true;
}

public enum BeneficioOrigin
{
    /// <summary>Granted by the system: cuponera or sello when a service closes.</summary>
    Automatico,
    /// <summary>Granted by hand by the office.</summary>
    Oficina,
}

/// <summary>
/// A sello or cupón granted to an invitado, with a unique code (the POS reads it in phase 2). It is reserved for a
/// pedido when the service is asked for, used when its recolección closes, and freed if the pedido is cancelled.
/// </summary>
public class Beneficio : Entity, IActivatable
{
    public int PromocionId { get; set; }
    public Promocion? Promocion { get; set; }
    public int InvitadoId { get; set; }
    public string Code { get; set; } = "";
    public BeneficioOrigin Origin { get; set; }
    /// <summary>Pedido whose recolección granted it (automatic ones).</summary>
    public int? GrantedByPedidoId { get; set; }
    /// <summary>
    /// Last day it can be used (Mexico City). Null while it lasts until the entrega of its pedido and that entrega is not
    /// programmed yet.
    /// </summary>
    public DateOnly? ExpiresOn { get; set; }
    /// <summary>Pedido it is reserved for, or where it was used.</summary>
    public int? PedidoId { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public bool Active { get; set; } = true;
}
