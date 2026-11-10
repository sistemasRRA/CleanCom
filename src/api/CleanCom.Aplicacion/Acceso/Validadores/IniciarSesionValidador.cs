using FluentValidation;

namespace CleanCom.Aplicacion.Acceso;

public class IniciarSesionValidador : AbstractValidator<IniciarSesionRequest>
{
    public IniciarSesionValidador()
    {
        RuleFor(x => x.Usuario).NotEmpty().WithMessage("Indica tu usuario o correo.");
        RuleFor(x => x.Contrasena).NotEmpty().WithMessage("Indica tu contraseña.");
    }
}
