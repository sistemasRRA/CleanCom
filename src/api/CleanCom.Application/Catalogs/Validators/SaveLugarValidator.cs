using FluentValidation;

namespace CleanCom.Application.Catalogs;

public class SaveLugarValidator : AbstractValidator<SaveLugarRequest>
{
    public SaveLugarValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).WithName("Nombre");
        RuleFor(x => x.Type).IsInEnum().WithName("Tipo");
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300).WithName("Dirección");
        RuleFor(x => x.OpeningHours).MaximumLength(200).WithName("Horario");
        RuleFor(x => x).Must(x => (x.Lat is null) == (x.Lng is null)).WithMessage("Indica latitud y longitud juntas.");
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90).WithName("Latitud").When(x => x.Lat is not null);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180).WithName("Longitud").When(x => x.Lng is not null);
    }
}
