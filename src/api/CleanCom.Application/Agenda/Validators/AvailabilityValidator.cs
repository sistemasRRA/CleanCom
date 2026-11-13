using FluentValidation;

namespace CleanCom.Application.Agenda;

public class AvailabilityValidator : AbstractValidator<AvailabilityRequest>
{
    public AvailabilityValidator()
    {
        RuleFor(x => x)
            .Must(x => x.DireccionId is not null || x.ZonaId is not null || (x.Lat is not null && x.Lng is not null)
                || !string.IsNullOrWhiteSpace(x.PostalCode))
            .WithName("Dirección")
            .WithMessage("Indica la dirección: ubicación, código postal o zona.");
        RuleFor(x => x.Lng).NotNull().When(x => x.Lat is not null).WithMessage("Falta la longitud de la ubicación.");
        RuleFor(x => x.Lat).NotNull().When(x => x.Lng is not null).WithMessage("Falta la latitud de la ubicación.");
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90).When(x => x.Lat is not null).WithName("Latitud");
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180).When(x => x.Lng is not null).WithName("Longitud");
        RuleFor(x => x.PostalCode).Matches("^[0-9]{5}$").When(x => !string.IsNullOrWhiteSpace(x.PostalCode))
            .WithMessage("El código postal debe tener 5 dígitos.");
        RuleFor(x => x.Turno).IsInEnum().WithName("Turno");
    }
}
