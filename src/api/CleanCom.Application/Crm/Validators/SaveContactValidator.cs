using FluentValidation;

namespace CleanCom.Application.Crm;

public class SaveContactValidator : AbstractValidator<SaveContactRequest>
{
    public SaveContactValidator()
    {
        RuleFor(x => x.Channel).IsInEnum().WithName("Canal");
        RuleFor(x => x.Result).IsInEnum().WithName("Resultado");
        RuleFor(x => x.Note).MaximumLength(1000).WithName("Nota");
    }
}
