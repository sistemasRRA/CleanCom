using FluentValidation;

namespace CleanCom.Application.Access;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithName("Contraseña actual");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .NotEqual(x => x.CurrentPassword).WithMessage("La contraseña nueva debe ser distinta de la actual.");
    }
}
