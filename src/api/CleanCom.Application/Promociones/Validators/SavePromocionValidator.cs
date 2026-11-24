using CleanCom.Domain.Promociones;
using FluentValidation;

namespace CleanCom.Application.Promociones;

public class SavePromocionValidator : AbstractValidator<SavePromocionRequest>
{
    public SavePromocionValidator()
    {
        RuleFor(x => x.Programa).IsInEnum().WithName("Programa");
        RuleFor(x => x.Programa).NotEqual(PromocionPrograma.Campana)
            .WithMessage("Las campañas llegan con las notificaciones; por ahora captura sellos o cupones.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithName("Nombre");
        RuleFor(x => x.DiscountType).IsInEnum().WithName("Tipo de descuento");
        RuleFor(x => x.Value).InclusiveBetween(1, 100).When(x => x.DiscountType == DiscountType.Porcentaje)
            .WithMessage("El porcentaje va de 1 a 100.");
        RuleFor(x => x.Value).GreaterThan(0).When(x => x.DiscountType == DiscountType.Monto)
            .WithMessage("Indica el monto del descuento.");
        RuleFor(x => x.Value).GreaterThan(0).When(x => x.DiscountType == DiscountType.PrecioEspecial)
            .WithMessage("Indica el precio especial.");
        RuleFor(x => x.Description).MaximumLength(300).WithName("Mecánica");
        RuleFor(x => x.MinimumAmount).GreaterThan(0).When(x => x.MinimumAmount is not null).WithName("Mínimo");
        RuleFor(x => x.PaymentMethod).IsInEnum().When(x => x.PaymentMethod is not null).WithName("Forma de pago");
        RuleFor(x => x.Condition).MaximumLength(500).WithName("Condición");
        RuleFor(x => x.ValidityDays).InclusiveBetween(1, 365).When(x => x.ValidityDays is not null).WithName("Vigencia (días)");
        RuleFor(x => x.ValidityDays).NotNull().When(x => x.Programa != PromocionPrograma.Sellos)
            .WithMessage("Indica la vigencia en días; \"hasta la entrega del pedido\" solo aplica a los sellos.");
        RuleFor(x => x.LineaServicioId).NotNull().When(x => x.Programa == PromocionPrograma.Cuponera)
            .WithMessage("Cada cupón de la cuponera es de una línea de servicio.");
        RuleFor(x => x.TriggerLineaServicioId).Null().When(x => x.Programa != PromocionPrograma.Sellos)
            .WithMessage("La línea que da el sello solo aplica a los sellos.");
    }
}
