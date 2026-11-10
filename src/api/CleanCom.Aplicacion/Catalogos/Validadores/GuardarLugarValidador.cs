using FluentValidation;

namespace CleanCom.Aplicacion.Catalogos;

public class GuardarLugarValidador : AbstractValidator<GuardarLugarRequest>
{
    public GuardarLugarValidador()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Direccion).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Horario).MaximumLength(200);
        RuleFor(x => x).Must(x => (x.Lat is null) == (x.Lng is null)).WithMessage("Indica latitud y longitud juntas.");
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90).When(x => x.Lat is not null);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180).When(x => x.Lng is not null);
    }
}
