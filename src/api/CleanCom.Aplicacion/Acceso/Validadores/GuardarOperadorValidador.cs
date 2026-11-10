using FluentValidation;

namespace CleanCom.Aplicacion.Acceso;

public class GuardarOperadorValidador : AbstractValidator<GuardarOperadorRequest>
{
    public GuardarOperadorValidador()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Telefono).MaximumLength(20);
    }
}
