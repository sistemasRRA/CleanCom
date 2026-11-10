using FluentValidation;

namespace CleanCom.Aplicacion.Catalogos;

public class GuardarRutaValidador : AbstractValidator<GuardarRutaRequest>
{
    public GuardarRutaValidador()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(10).Matches("^[A-Z0-9]+$").WithMessage("El código va en mayúsculas, sin espacios (por ejemplo F o K).");
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$").WithMessage("El color va en formato #RRGGBB.");
        RuleFor(x => x.CupoParadas).GreaterThan(0).When(x => x.CupoParadas is not null);
        RuleFor(x => x.MetaRecolecciones).GreaterThan(0).When(x => x.MetaRecolecciones is not null);
    }
}
