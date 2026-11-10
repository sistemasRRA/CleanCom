using CleanCom.Dominio.Acceso;
using FluentValidation;

namespace CleanCom.Aplicacion.Acceso;

public class CrearUsuarioValidador : AbstractValidator<CrearUsuarioRequest>
{
    public CrearUsuarioValidador()
    {
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Usuario).NotEmpty().MaximumLength(100).Matches("^[a-zA-Z0-9._@-]+$")
            .WithMessage("El usuario solo lleva letras, números, punto, guion y arroba (por ejemplo juan.rosales).");
        RuleFor(x => x.Correo).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Correo));
        RuleFor(x => x.Telefono).MaximumLength(20);
        RuleFor(x => x.Rol).Must(r => r is Roles.Administracion or Roles.Chofer)
            .WithMessage("El rol debe ser Administracion o Chofer. Los invitados se registran desde su app.");
        RuleFor(x => x.ContrasenaInicial).NotEmpty().MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");
        RuleFor(x => x).Must(x => x.OperadorId is null || !x.CrearOperador)
            .WithMessage("Elige un operador existente o crea uno nuevo, no ambos.");
        RuleFor(x => x).Must(x => x.Rol == Roles.Chofer || (x.OperadorId is null && !x.CrearOperador))
            .WithMessage("Solo un usuario Chofer se liga a un operador.");
    }
}
