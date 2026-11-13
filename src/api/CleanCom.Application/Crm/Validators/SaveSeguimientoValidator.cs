using FluentValidation;

namespace CleanCom.Application.Crm;

public class SaveSeguimientoValidator : AbstractValidator<SaveSeguimientoRequest>
{
    public SaveSeguimientoValidator()
    {
        RuleFor(x => x.ManualStatus).IsInEnum().When(x => x.ManualStatus is not null).WithName("Estatus");
        RuleFor(x => x.FixedFrequencyDays).InclusiveBetween(1, 365).When(x => x.FixedFrequencyDays is not null).WithName("Frecuencia fija");
    }
}
