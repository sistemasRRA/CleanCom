using FluentValidation;

namespace CleanCom.Aplicacion.Catalogos;

public class GuardarLineaServicioValidador : AbstractValidator<GuardarLineaServicioRequest>
{
    public GuardarLineaServicioValidador()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DiasProceso).GreaterThan(0).When(x => x.DiasProceso is not null);
    }
}
