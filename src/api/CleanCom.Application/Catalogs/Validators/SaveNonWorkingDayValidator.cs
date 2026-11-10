using FluentValidation;

namespace CleanCom.Application.Catalogs;

public class SaveNonWorkingDayValidator : AbstractValidator<SaveNonWorkingDayRequest>
{
    public SaveNonWorkingDayValidator()
    {
        RuleFor(x => x.Date).NotEmpty().WithName("Fecha");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200).WithName("Motivo");
    }
}
