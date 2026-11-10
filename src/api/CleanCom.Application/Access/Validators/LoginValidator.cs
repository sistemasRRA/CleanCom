using FluentValidation;

namespace CleanCom.Application.Access;

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("Indica tu usuario o correo.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Indica tu contraseña.");
    }
}
