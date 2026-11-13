using FluentValidation;

namespace CleanCom.Application.Crm;

public class SaveContactoValidator : AbstractValidator<SaveContactoRequest>
{
    public SaveContactoValidator()
    {
        RuleFor(x => x.Channel).IsInEnum().WithName("Canal");
        RuleFor(x => x.Result).IsInEnum().WithName("Resultado");
        RuleFor(x => x.Note).MaximumLength(1000).WithName("Nota");
    }
}
