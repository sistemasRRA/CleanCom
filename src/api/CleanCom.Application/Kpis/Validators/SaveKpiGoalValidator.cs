using FluentValidation;

namespace CleanCom.Application.Kpis;

public class SaveKpiGoalValidator : AbstractValidator<SaveKpiGoalRequest>
{
    private const decimal Max = 999_999_999m;

    public SaveKpiGoalValidator()
    {
        RuleFor(x => x.Period).IsInEnum().WithName("Periodo");
        RuleFor(x => x.Red).InclusiveBetween(-Max, Max).When(x => x.Red is not null).WithName("Rojo");
        RuleFor(x => x.Green).InclusiveBetween(-Max, Max).WithName("Verde");
        RuleFor(x => x.SuperGreen).InclusiveBetween(-Max, Max).When(x => x.SuperGreen is not null).WithName("Súper verde");
    }
}
