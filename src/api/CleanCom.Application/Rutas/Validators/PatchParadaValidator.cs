using FluentValidation;

namespace CleanCom.Application.Rutas;

public class PatchParadaValidator : AbstractValidator<PatchParadaRequest>
{
    public PatchParadaValidator()
    {
        RuleFor(x => x).Must(x => x.Position is not null || x.Fixed is not null).WithMessage("Indica la nueva posición o si queda fija.");
        RuleFor(x => x.Position).GreaterThan(0).WithName("Posición").When(x => x.Position is not null);
    }
}
