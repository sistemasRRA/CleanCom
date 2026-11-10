using FluentValidation;

namespace CleanCom.Aplicacion.Catalogos;

public class GuardarCoberturaValidador : AbstractValidator<GuardarCoberturaRequest>
{
    public GuardarCoberturaValidador()
    {
        RuleFor(x => x.CodigoPostal).NotEmpty().Matches("^[0-9]{5}$").WithMessage("El código postal tiene 5 dígitos.");
        RuleFor(x => x.Colonia).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Alcaldia).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ZonaId).GreaterThan(0);
    }
}
