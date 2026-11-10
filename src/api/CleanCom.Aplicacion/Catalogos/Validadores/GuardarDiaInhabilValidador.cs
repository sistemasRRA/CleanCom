using FluentValidation;

namespace CleanCom.Aplicacion.Catalogos;

public class GuardarDiaInhabilValidador : AbstractValidator<GuardarDiaInhabilRequest>
{
    public GuardarDiaInhabilValidador()
    {
        RuleFor(x => x.Fecha).NotEmpty();
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(200);
    }
}
