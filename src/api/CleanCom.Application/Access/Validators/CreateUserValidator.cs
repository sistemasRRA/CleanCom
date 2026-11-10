using CleanCom.Domain.Access;
using FluentValidation;

namespace CleanCom.Application.Access;

public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150).WithName("Nombre completo");
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(100).WithName("Usuario").Matches("^[a-zA-Z0-9._@-]+$")
            .WithMessage("El usuario solo lleva letras, números, punto, guion y arroba (por ejemplo juan.rosales).");
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).WithName("Correo").When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20).WithName("Teléfono");
        RuleFor(x => x.Role).NotEmpty().WithName("Rol");
        RuleFor(x => x.Role).Must(Roles.IsAssignableInOffice)
            .WithMessage("Los invitados se registran desde su app; elige un rol de oficina.");
        RuleFor(x => x.InitialPassword).NotEmpty().MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");
        RuleFor(x => x).Must(x => x.OperadorId is null || !x.CreateOperador)
            .WithMessage("Elige un operador existente o crea uno nuevo, no ambos.");
        RuleFor(x => x).Must(x => x.Role == Roles.Chofer || (x.OperadorId is null && !x.CreateOperador))
            .WithMessage("Solo un usuario Chofer se liga a un operador.");
    }
}
