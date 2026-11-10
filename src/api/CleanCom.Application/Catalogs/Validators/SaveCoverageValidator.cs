using FluentValidation;

namespace CleanCom.Application.Catalogs;

public class SaveCoverageValidator : AbstractValidator<SaveCoverageRequest>
{
    public SaveCoverageValidator()
    {
        RuleFor(x => x.PostalCode).NotEmpty().Matches("^[0-9]{5}$").WithMessage("El código postal tiene 5 dígitos.");
        RuleFor(x => x.Neighborhood).NotEmpty().MaximumLength(150).WithName("Colonia");
        RuleFor(x => x.Borough).NotEmpty().MaximumLength(100).WithName("Alcaldía");
        RuleFor(x => x.ZonaId).GreaterThan(0).WithName("Zona");
    }
}
