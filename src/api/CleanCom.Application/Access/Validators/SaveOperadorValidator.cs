using FluentValidation;

namespace CleanCom.Application.Access;

public class SaveOperadorValidator : AbstractValidator<SaveOperadorRequest>
{
    public SaveOperadorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).WithName("Nombre");
        RuleFor(x => x.Phone).MaximumLength(20).WithName("Teléfono");
    }
}
