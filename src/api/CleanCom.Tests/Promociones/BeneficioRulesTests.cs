using CleanCom.Domain.Common;
using CleanCom.Domain.People;
using CleanCom.Domain.Promociones;
using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Promociones;

/// <summary>Promociones (docs/03-reglas-de-negocio.md): sellos, cuponera, códigos, apartar y canjear.</summary>
public class BeneficioRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);
    private const int Tintoreria = 1;
    private const int Lavanderia = 2;

    private static Promocion Promo(int id, PromocionPrograma programa = PromocionPrograma.Sellos, int? linea = null, int? trigger = null,
        bool active = true, bool stackable = false) =>
        new() { Id = id, Name = $"Promo {id}", Programa = programa, LineaServicioId = linea, TriggerLineaServicioId = trigger, Active = active, Stackable = stackable, ValidityDays = 60 };

    private static Beneficio Available(DateOnly? expires = null) => new() { Code = "CC-ABCD-EFGH", ExpiresOn = expires ?? Today.AddDays(30) };

    [Fact]
    public void Codes_are_unique_looking_and_avoid_confusing_characters()
    {
        var random = new Random(7);
        var codes = Enumerable.Range(0, 500).Select(_ => BeneficioRules.NewCode(random.Next)).ToList();
        Assert.All(codes, c => Assert.Matches("^CC-[2-9A-HJKMNP-Z]{4}-[2-9A-HJKMNP-Z]{4}$", c));
        Assert.Equal(codes.Count, codes.Distinct().Count());
        Assert.Equal("CC-7KQ4-M2XP", BeneficioRules.NormalizeCode(" cc-7kq4-m2xp "));
    }

    [Fact]
    public void The_cuponera_lasts_60_days_from_the_day_it_is_granted()
    {
        // Granted on 24 nov with 60 days → it can be used until 23 jan.
        Assert.Equal(new DateOnly(2027, 1, 23), BeneficioRules.ExpiresOn(Today, 60, null));
    }

    [Fact]
    public void A_sello_until_the_entrega_expires_on_the_entrega_of_its_pedido()
    {
        // "Hasta la entrega del pedido": with the entrega programmed on 28 nov it is valid until that day; without an
        // entrega yet it has no date and does not expire.
        var entrega = new DateOnly(2026, 11, 28);
        Assert.Equal(entrega, BeneficioRules.ExpiresOn(Today, null, entrega));
        Assert.Null(BeneficioRules.ExpiresOn(Today, null, null));
        var open = new Beneficio { Code = "X", ExpiresOn = null };
        Assert.Equal(BeneficioState.Disponible, BeneficioRules.State(open, Today.AddDays(400)));
        BeneficioRules.EnsureCanReserve(open, Promo(1, linea: Tintoreria), "Tintorería", Today, Today.AddDays(10), [Tintoreria], []);
        Assert.Equal(BeneficioState.Vencido, BeneficioRules.State(new Beneficio { ExpiresOn = entrega }, entrega.AddDays(1)));
    }

    [Fact]
    public void The_demo_sellos_last_until_the_entrega_and_the_cuponera_waits_inactive_for_its_benefit()
    {
        var sellos = PromocionDefaults.Sellos();
        Assert.Equal(5, sellos.Count);
        Assert.All(sellos, p => Assert.Null(p.ValidityDays));
        Assert.All(sellos, p => Assert.False(p.Stackable));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, sellos.Select(p => p.LineaServicioId!.Value));
        var cuponera = PromocionDefaults.Cuponera();
        Assert.Equal(5, cuponera.Count);
        Assert.All(cuponera, p => Assert.False(p.Active));
        Assert.All(cuponera, p => Assert.Equal(60, p.ValidityDays));
        Assert.Empty(BeneficioRules.WelcomeCoupons(cuponera));
    }

    [Fact]
    public void State_follows_use_reservation_and_expiry()
    {
        Assert.Equal(BeneficioState.Disponible, BeneficioRules.State(Available(), Today));
        Assert.Equal(BeneficioState.Disponible, BeneficioRules.State(Available(Today), Today));
        Assert.Equal(BeneficioState.Vencido, BeneficioRules.State(Available(Today.AddDays(-1)), Today));
        Assert.Equal(BeneficioState.Apartado, BeneficioRules.State(new Beneficio { PedidoId = 5, ExpiresOn = Today.AddDays(-1) }, Today));
        Assert.Equal(BeneficioState.Usado, BeneficioRules.State(new Beneficio { PedidoId = 5, UsedAt = DateTimeOffset.UtcNow, ExpiresOn = Today }, Today));
    }

    [Fact]
    public void The_cuponera_gives_one_coupon_of_each_active_promotion_and_none_inactive()
    {
        var promos = new[]
        {
            Promo(1, PromocionPrograma.Cuponera, Tintoreria),
            Promo(2, PromocionPrograma.Cuponera, Lavanderia),
            Promo(3, PromocionPrograma.Cuponera, 3, active: false),
            Promo(4, PromocionPrograma.Sellos),
        };
        Assert.Equal([1, 2], BeneficioRules.WelcomeCoupons(promos).Select(p => p.Id));
    }

    [Fact]
    public void A_sello_is_granted_by_its_trigger_linea_and_not_twice_while_unused()
    {
        var promos = new[]
        {
            Promo(1, trigger: Lavanderia),   // "al cerrar un servicio de Lavandería"
            Promo(2, trigger: Tintoreria),
            Promo(3),                        // any service
            Promo(4, trigger: Lavanderia, active: false),
            Promo(5, PromocionPrograma.Cuponera),
        };
        Assert.Equal([1, 3], BeneficioRules.SellosFor(promos, [Lavanderia], new HashSet<int>()).Select(p => p.Id));
        Assert.Equal([3], BeneficioRules.SellosFor(promos, [Lavanderia], new HashSet<int> { 1 }).Select(p => p.Id));
        Assert.Equal([2, 3], BeneficioRules.SellosFor(promos, [Tintoreria], new HashSet<int>()).Select(p => p.Id));
    }

    [Fact]
    public void Tiendas_do_not_take_part()
    {
        Assert.True(BeneficioRules.TakesPart(InvitadoType.ClienteFinal));
        Assert.True(BeneficioRules.TakesPart(InvitadoType.Empresa));
        Assert.False(BeneficioRules.TakesPart(InvitadoType.TiendaInterna));
    }

    [Fact]
    public void A_beneficio_is_reserved_only_if_available_valid_and_of_a_linea_in_the_pedido()
    {
        var promo = Promo(1, linea: Tintoreria);
        BeneficioRules.EnsureCanReserve(Available(), promo, "Tintorería", Today, Today.AddDays(2), [Tintoreria], []);

        Assert.Contains("ya se usó", Reserve(new Beneficio { Code = "X", UsedAt = DateTimeOffset.UtcNow, ExpiresOn = Today }, promo));
        Assert.Contains("apartado para el pedido 9", Reserve(new Beneficio { Code = "X", PedidoId = 9, ExpiresOn = Today }, promo));
        Assert.Contains("venció", Reserve(Available(Today.AddDays(-1)), promo));
        // Valid today but expires before the recolección.
        Assert.Contains("antes de la recolección", Reserve(Available(Today.AddDays(1)), promo, recoleccion: Today.AddDays(2)));
        Assert.Contains("es de Tintorería", Reserve(Available(), promo, lineas: [Lavanderia]));
    }

    [Fact]
    public void Only_stackable_beneficios_go_together_on_a_pedido()
    {
        var single = Promo(1);
        var stackable = Promo(2, stackable: true);
        var otherStackable = Promo(3, stackable: true);
        BeneficioRules.EnsureCanReserve(Available(), stackable, null, Today, Today, [], [otherStackable]);
        Assert.Contains("no se puede combinar", Reserve(Available(), single, others: [otherStackable]));
        Assert.Contains("no se puede combinar", Reserve(Available(), stackable, others: [single]));
    }

    [Fact]
    public void A_used_or_expired_beneficio_cannot_be_used_again()
    {
        BeneficioRules.EnsureCanUse(Available(Today), Today);
        Assert.Contains("ya se usó", Assert.Throws<DomainException>(() =>
            BeneficioRules.EnsureCanUse(new Beneficio { Code = "X", UsedAt = DateTimeOffset.UtcNow, ExpiresOn = Today }, Today)).Message);
        Assert.Contains("venció", Assert.Throws<DomainException>(() => BeneficioRules.EnsureCanUse(Available(Today.AddDays(-1)), Today)).Message);
    }

    [Fact]
    public void Redemption_rate_and_the_30_percent_cap_of_a_single_sello()
    {
        Assert.Null(BeneficioRules.RedemptionRate(0, 0));
        Assert.Equal(40m, BeneficioRules.RedemptionRate(10, 4));
        Assert.Equal(33.3m, BeneficioRules.RedemptionRate(3, 1));
        Assert.False(BeneficioRules.ExceedsShare(3, 10, 30));
        Assert.True(BeneficioRules.ExceedsShare(4, 10, 30));
        Assert.False(BeneficioRules.ExceedsShare(0, 0, 30));
    }

    [Fact]
    public void The_mechanic_reads_in_spanish()
    {
        var edredon = new Promocion { DiscountType = DiscountType.Porcentaje, Value = 20 };
        Assert.Equal("20% de descuento en Tintorería · no acumulable", BeneficioRules.Mechanic(edredon, "Tintorería"));
        var monto = new Promocion { DiscountType = DiscountType.Monto, Value = 1500, MinimumAmount = 3000, PaymentMethod = PaymentMethod.Efectivo, Stackable = true };
        Assert.Equal("$1,500 de descuento · mínimo $3,000 · solo efectivo", BeneficioRules.Mechanic(monto, null));
        Assert.Equal("Artículo gratis en Lavandería · no acumulable",
            BeneficioRules.Mechanic(new Promocion { DiscountType = DiscountType.ArticuloGratis }, "Lavandería"));
        Assert.Equal("Precio especial de $100 en Calzado · no acumulable",
            BeneficioRules.Mechanic(new Promocion { DiscountType = DiscountType.PrecioEspecial, Value = 100 }, "Calzado"));
        // A written mechanic is shown as is.
        Assert.Equal("1 kilo de lavandería gratis al traer 5 kilos o más.",
            BeneficioRules.Mechanic(new Promocion { Description = "1 kilo de lavandería gratis al traer 5 kilos o más." }, "Lavandería"));
    }

    private static string Reserve(Beneficio b, Promocion p, DateOnly? recoleccion = null, int[]? lineas = null, Promocion[]? others = null) =>
        Assert.Throws<DomainException>(() =>
            BeneficioRules.EnsureCanReserve(b, p, "Tintorería", Today, recoleccion ?? Today, lineas ?? [Tintoreria], others ?? [])).Message;
}
