using FluentValidation;

namespace CleanCom.Application.Catalogs;

public class SaveLineaServicioValidator : AbstractValidator<SaveLineaServicioRequest>
{
    public SaveLineaServicioValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithName("Nombre");
        RuleFor(x => x.ProcessingDays).GreaterThan(0).WithName("Días de proceso").When(x => x.ProcessingDays is not null);
    }
}
