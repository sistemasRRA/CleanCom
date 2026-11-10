using FluentValidation;

namespace CleanCom.Application.Catalogs;

public class SaveRutaValidator : AbstractValidator<SaveRutaRequest>
{
    public SaveRutaValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10).WithName("Código").Matches("^[A-Z0-9]+$")
            .WithMessage("El código va en mayúsculas, sin espacios (por ejemplo F o K).");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithName("Nombre");
        RuleFor(x => x.Color).NotEmpty().WithName("Color").Matches("^#[0-9A-Fa-f]{6}$").WithMessage("El color va en formato #RRGGBB.");
        RuleFor(x => x.CupoParadas).GreaterThan(0).WithName("Cupo de paradas").When(x => x.CupoParadas is not null);
        RuleFor(x => x.RecoleccionGoal).GreaterThan(0).WithName("Meta de recolecciones").When(x => x.RecoleccionGoal is not null);
    }
}
