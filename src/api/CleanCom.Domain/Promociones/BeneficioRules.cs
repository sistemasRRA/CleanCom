using System.Globalization;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Common;
using CleanCom.Domain.People;

namespace CleanCom.Domain.Promociones;

public enum BeneficioState
{
    Disponible,
    /// <summary>Reserved for a pedido that has not been collected yet.</summary>
    Apartado,
    Usado,
    Vencido,
}

/// <summary>
/// Promociones rules (docs/03-reglas-de-negocio.md, Promociones): granting, reserving, using and the indicators.
/// </summary>
public static class BeneficioRules
{
    private static readonly CultureInfo Spanish = new("es-MX");

    /// <summary>Letters and digits that cannot be confused when read aloud or typed (no 0/O, 1/I/L).</summary>
    private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    /// <summary>A code like "CC-7KQ4-M2XP"; <paramref name="next"/> gives a random index below its argument.</summary>
    public static string NewCode(Func<int, int> next)
    {
        var chars = Enumerable.Range(0, 8).Select(_ => Alphabet[next(Alphabet.Length)]).ToArray();
        return $"CC-{new string(chars, 0, 4)}-{new string(chars, 4, 4)}";
    }

    /// <summary>Typed codes are compared without spaces and in upper case.</summary>
    public static string NormalizeCode(string code) => new string(code.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

    /// <summary>
    /// Last day it can be used: <paramref name="validityDays"/> days after it was granted or, without days ("hasta la
    /// entrega del pedido"), the entrega date of the pedido that gave it (null while not programmed).
    /// </summary>
    public static DateOnly? ExpiresOn(DateOnly granted, int? validityDays, DateOnly? entrega) =>
        validityDays is { } days ? granted.AddDays(days) : entrega;

    public static bool Expired(Beneficio b, DateOnly day) => b.ExpiresOn is { } last && last < day;

    public static BeneficioState State(Beneficio b, DateOnly today) =>
        b.UsedAt is not null ? BeneficioState.Usado
        : b.PedidoId is not null ? BeneficioState.Apartado
        : Expired(b, today) ? BeneficioState.Vencido
        : BeneficioState.Disponible;

    /// <summary>Tiendas (uso interno) do not take part in promotions, like in the CRM metrics.</summary>
    public static bool TakesPart(InvitadoType type) => type != InvitadoType.TiendaInterna;

    /// <summary>The cuponera when the first service of a new invitado closes: one cupón of each active promotion.</summary>
    public static IReadOnlyList<Promocion> WelcomeCoupons(IEnumerable<Promocion> promociones) =>
        promociones.Where(p => p.Active && p.Programa == PromocionPrograma.Cuponera).ToList();

    /// <summary>
    /// Sellos when a later service closes: the active ones whose trigger línea is in the pedido (or without a trigger),
    /// except those the invitado still holds unused.
    /// </summary>
    public static IReadOnlyList<Promocion> SellosFor(IEnumerable<Promocion> promociones, IReadOnlyCollection<int> pedidoLineas, IReadOnlySet<int> heldPromociones) =>
        promociones.Where(p => p.Active && p.Programa == PromocionPrograma.Sellos
                && (p.TriggerLineaServicioId is null || pedidoLineas.Contains(p.TriggerLineaServicioId.Value))
                && !heldPromociones.Contains(p.Id))
            .ToList();

    /// <summary>
    /// Whether a beneficio can be reserved for a pedido: available, valid on the recolección day, of a línea in the pedido,
    /// and combinable with the others reserved for it.
    /// </summary>
    /// <param name="linea">Name of the promotion's línea, when it has one.</param>
    public static void EnsureCanReserve(Beneficio b, Promocion p, string? linea, DateOnly today, DateOnly recoleccion,
        IReadOnlyCollection<int> pedidoLineas, IReadOnlyCollection<Promocion> others)
    {
        switch (State(b, today))
        {
            case BeneficioState.Usado:
                throw new DomainException($"El beneficio {b.Code} ya se usó.");
            case BeneficioState.Apartado:
                throw new DomainException($"El beneficio {b.Code} ya está apartado para el pedido {b.PedidoId}.");
            case BeneficioState.Vencido:
                throw new DomainException($"El beneficio {b.Code} venció el {Format(b.ExpiresOn!.Value)}.");
        }
        if (Expired(b, recoleccion))
        {
            throw new DomainException($"{p.Name} vence el {Format(b.ExpiresOn!.Value)}, antes de la recolección del {Format(recoleccion)}.");
        }
        if (p.LineaServicioId is { } lineaId && !pedidoLineas.Contains(lineaId))
        {
            throw new DomainException($"{p.Name} es de {linea ?? "otra línea de servicio"}; agrégala al pedido para usarlo.");
        }
        if (others.Count > 0 && (!p.Stackable || others.Any(o => !o.Stackable)))
        {
            throw new DomainException($"{p.Name} no se puede combinar con otro beneficio en el mismo pedido.");
        }
    }

    /// <summary>Marking by its code (office or chofer): only an unused beneficio that has not expired.</summary>
    public static void EnsureCanUse(Beneficio b, DateOnly today)
    {
        if (b.UsedAt is not null)
        {
            throw new DomainException($"El beneficio {b.Code} ya se usó.");
        }
        if (Expired(b, today))
        {
            throw new DomainException($"El beneficio {b.Code} venció el {Format(b.ExpiresOn!.Value)}.");
        }
    }

    /// <summary>Used ÷ granted in percent; null while nothing was granted.</summary>
    public static decimal? RedemptionRate(int granted, int used) =>
        granted == 0 ? null : Math.Round(used * 100m / granted, 1);

    /// <summary>"Un solo sello no debe pasar del 30% de los otorgados" (Promociones.TopeSello).</summary>
    public static bool ExceedsShare(int grantedOfSello, int totalSellosGranted, int maxPercent) =>
        totalSellosGranted > 0 && grantedOfSello * 100m / totalSellosGranted > maxPercent;

    /// <summary>
    /// The mechanic in Spanish: its description when it has one, otherwise from the rules: "20% de descuento en
    /// Tintorería", "$50 de descuento · mínimo $300 · solo efectivo".
    /// </summary>
    public static string Mechanic(Promocion p, string? linea)
    {
        if (!string.IsNullOrWhiteSpace(p.Description))
        {
            return p.Description;
        }
        var discount = p.DiscountType switch
        {
            DiscountType.Porcentaje => $"{p.Value.ToString("0.##", Spanish)}% de descuento",
            DiscountType.Monto => $"${p.Value.ToString("#,0.##", Spanish)} de descuento",
            DiscountType.PrecioEspecial => $"Precio especial de ${p.Value.ToString("#,0.##", Spanish)}",
            _ => "Artículo gratis",
        };
        var parts = new List<string> { linea is null ? discount : $"{discount} en {linea}" };
        if (p.MinimumAmount is { } minimum)
        {
            parts.Add($"mínimo ${minimum.ToString("#,0.##", Spanish)}");
        }
        if (p.PaymentMethod is { } method)
        {
            parts.Add($"solo {method.ToString().ToLowerInvariant()}");
        }
        if (!p.Stackable)
        {
            parts.Add("no acumulable");
        }
        return string.Join(" · ", parts);
    }

    private static string Format(DateOnly date) => AgendaCalculator.Format(date);
}
