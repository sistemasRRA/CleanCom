using FluentValidation;

namespace CleanCom.Application.Invitados;

public class SaveDireccionValidator : AbstractValidator<SaveDireccionRequest>
{
    public SaveDireccionValidator()
    {
        RuleFor(x => x.Label).MaximumLength(50).WithName("Etiqueta");
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200).WithName("Calle");
        RuleFor(x => x.ExteriorNumber).MaximumLength(20).WithName("Número exterior");
        RuleFor(x => x.InteriorNumber).MaximumLength(20).WithName("Número interior");
        RuleFor(x => x.PostalCode).NotEmpty().Matches("^[0-9]{5}$").WithName("Código postal")
            .WithMessage("El código postal debe tener 5 dígitos.");
        RuleFor(x => x.Neighborhood).MaximumLength(150).WithName("Colonia");
        RuleFor(x => x.Borough).MaximumLength(100).WithName("Alcaldía");
        RuleFor(x => x.References).MaximumLength(500).WithName("Referencias");
        RuleFor(x => x.Lng).NotNull().When(x => x.Lat is not null).WithMessage("Falta la longitud de la ubicación.");
        RuleFor(x => x.Lat).NotNull().When(x => x.Lng is not null).WithMessage("Falta la latitud de la ubicación.");
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90).When(x => x.Lat is not null).WithName("Latitud");
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180).When(x => x.Lng is not null).WithName("Longitud");
        RuleFor(x => x.ZonaId).GreaterThan(0).When(x => x.ZonaId is not null).WithName("Zona");
    }
}
