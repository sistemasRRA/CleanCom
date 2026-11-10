using CleanCom.Dominio.Acceso;
using FluentValidation;

namespace CleanCom.Aplicacion.Acceso;

public class ActualizarUsuarioValidador : AbstractValidator<ActualizarUsuarioRequest>
{
    public ActualizarUsuarioValidador()
    {
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Correo).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Correo));
        RuleFor(x => x.Telefono).MaximumLength(20);
        RuleFor(x => x.Rol).Must(r => r is Roles.Administracion or Roles.Chofer)
            .WithMessage("El rol debe ser Administracion o Chofer.");
        RuleFor(x => x).Must(x => x.Rol == Roles.Chofer || x.OperadorId is null)
            .WithMessage("Solo un usuario Chofer se liga a un operador.");
    }
}
