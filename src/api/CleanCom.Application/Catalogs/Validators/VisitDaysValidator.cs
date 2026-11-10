using FluentValidation;

namespace CleanCom.Application.Catalogs;

public class VisitDaysValidator : AbstractValidator<VisitDaysRequest>
{
    public VisitDaysValidator()
    {
        RuleFor(x => x.Days).NotNull().WithName("Días");
        RuleForEach(x => x.Days).IsInEnum().WithName("Día");
    }
}
