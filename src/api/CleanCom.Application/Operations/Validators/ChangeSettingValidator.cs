using CleanCom.Domain.Operations;
using FluentValidation;

namespace CleanCom.Application.Operations;

public class ChangeSettingValidator : AbstractValidator<ChangeSettingRequest>
{
    public ChangeSettingValidator()
    {
        RuleFor(x => x.Key).NotEmpty().WithMessage("Indica la clave.")
            .Must(k => SettingCatalog.Find(k) is not null).WithMessage(x => $"La clave \"{x.Key}\" no existe.");
        RuleFor(x => x.Value).NotEmpty().WithMessage("Indica el valor.").MaximumLength(500).WithName("Valor");
        RuleFor(x => x.Note).MaximumLength(500).WithName("Nota");
    }
}
