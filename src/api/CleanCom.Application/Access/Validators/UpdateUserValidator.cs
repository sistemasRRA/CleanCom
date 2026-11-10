using CleanCom.Domain.Access;
using FluentValidation;

namespace CleanCom.Application.Access;

public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150).WithName("Nombre completo");
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).WithName("Correo").When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20).WithName("Teléfono");
        RuleFor(x => x.Role).NotEmpty().WithName("Rol");
        RuleFor(x => x.Role).Must(Roles.IsAssignableInOffice)
            .WithMessage("Los invitados se registran desde su app; elige un rol de oficina.");
        RuleFor(x => x).Must(x => x.Role == Roles.Chofer || x.OperadorId is null)
            .WithMessage("Solo un usuario Chofer se liga a un operador.");
    }
}
