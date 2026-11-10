using FluentValidation;

namespace CleanCom.Application.Catalogs;

public class SaveReasonValidator : AbstractValidator<SaveReasonRequest>
{
    public SaveReasonValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithName("Tipo");
        RuleFor(x => x.Text).NotEmpty().MaximumLength(200).WithName("Texto");
    }
}
