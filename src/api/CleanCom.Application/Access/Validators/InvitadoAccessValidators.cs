using FluentValidation;

namespace CleanCom.Application.Access;

public class SendCodeValidator : AbstractValidator<SendCodeRequest>
{
    public SendCodeValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20).WithName("Celular");
    }
}

public class VerifyCodeValidator : AbstractValidator<VerifyCodeRequest>
{
    public VerifyCodeValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20).WithName("Celular");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10).WithName("Código");
        RuleFor(x => x.Campaign).MaximumLength(100).WithName("Campaña");
    }
}
