using CleanCom.Domain.Promociones;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.Promociones;

/// <param name="Mechanic">The rules in Spanish: "20% de descuento en Tintorería · no acumulable".</param>
/// <param name="Granted">Beneficios granted (active ones).</param>
/// <param name="Reserved">Reserved for a pedido not collected yet.</param>
/// <param name="RedemptionRate">Used ÷ granted, in percent.</param>
/// <param name="SharePercent">Sellos: its share of all granted sellos, in percent.</param>
/// <param name="OverCap">Sellos: its share is over Promociones.TopeSello.</param>
public record PromocionDto(
    int Id,
    PromocionPrograma Programa,
    string Name,
    int? LineaServicioId,
    string? LineaName,
    DiscountType DiscountType,
    decimal Value,
    decimal? MinimumAmount,
    PaymentMethod? PaymentMethod,
    bool Stackable,
    int? TriggerLineaServicioId,
    string? TriggerLineaName,
    string? Description,
    string? Condition,
    int? ValidityDays,
    bool Active,
    string Mechanic,
    int Granted,
    int Used,
    int Reserved,
    decimal? RedemptionRate,
    decimal? SharePercent,
    bool OverCap);

/// <param name="Goal">Redemption goal in percent (Promociones.MetaCanjeSellos or MetaCanjeCuponera).</param>
public record ProgramaIndicatorDto(PromocionPrograma Programa, int Granted, int Used, decimal? RedemptionRate, int Goal, bool MeetsGoal);

/// <summary>Answer of <c>GET /promociones</c>: indicators per program and the promotions with their numbers.</summary>
/// <param name="SelloCap">Promociones.TopeSello, in percent.</param>
public record PromocionesBoardDto(IReadOnlyList<ProgramaIndicatorDto> Indicators, IReadOnlyList<PromocionDto> Promociones, int SelloCap);

/// <param name="TriggerLineaServicioId">Sellos only: granted when a service with this línea closes; empty = any service.</param>
/// <param name="Description">The mechanic as the invitado reads it; empty = written from the rules.</param>
/// <param name="ValidityDays">Days a granted beneficio is valid (60 for the cuponera); empty (sellos only) = until the
/// entrega of the pedido that gave it.</param>
public record SavePromocionRequest(
    PromocionPrograma Programa,
    string Name,
    int? LineaServicioId,
    DiscountType DiscountType,
    decimal Value,
    decimal? MinimumAmount,
    PaymentMethod? PaymentMethod,
    bool Stackable,
    int? TriggerLineaServicioId,
    string? Description,
    string? Condition,
    int? ValidityDays,
    bool Active = true);

/// <summary>A sello or cupón of an invitado, as the office, the chofer and the app show it.</summary>
/// <param name="ExpiresOn">Last day it can be used; empty while it lasts until the entrega of its pedido and that entrega
/// is not programmed yet.</param>
/// <param name="PedidoId">Pedido it is reserved for, or where it was used.</param>
public record BeneficioDto(
    int Id,
    string Code,
    int PromocionId,
    string Name,
    PromocionPrograma Programa,
    string Mechanic,
    string? Condition,
    int? LineaServicioId,
    string? LineaName,
    bool Stackable,
    BeneficioState State,
    BeneficioOrigin Origin,
    DateTimeOffset GrantedAt,
    DateOnly? ExpiresOn,
    int? PedidoId,
    DateTimeOffset? UsedAt);

public record GrantBeneficioRequest(int PromocionId);

/// <param name="Warning">When the sello is over Promociones.TopeSello: it was granted, but the office should know.</param>
public record GrantBeneficioResultDto(BeneficioDto Beneficio, string? Warning);
