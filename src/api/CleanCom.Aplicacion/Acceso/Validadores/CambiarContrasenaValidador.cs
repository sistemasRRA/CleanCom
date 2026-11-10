using FluentValidation;

namespace CleanCom.Aplicacion.Acceso;

public class CambiarContrasenaValidador : AbstractValidator<CambiarContrasenaRequest>
{
    public CambiarContrasenaValidador()
    {
        RuleFor(x => x.ContrasenaActual).NotEmpty();
        RuleFor(x => x.ContrasenaNueva).NotEmpty().MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .NotEqual(x => x.ContrasenaActual).WithMessage("La contraseña nueva debe ser distinta de la actual.");
    }
}
