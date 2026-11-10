using FluentValidation;

namespace CleanCom.Aplicacion.Acceso;

public class RestablecerContrasenaValidador : AbstractValidator<RestablecerContrasenaRequest>
{
    public RestablecerContrasenaValidador()
    {
        RuleFor(x => x.ContrasenaTemporal).NotEmpty().MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");
    }
}
