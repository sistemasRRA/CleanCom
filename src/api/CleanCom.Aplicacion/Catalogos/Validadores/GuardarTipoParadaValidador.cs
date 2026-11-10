using FluentValidation;

namespace CleanCom.Aplicacion.Catalogos;

public class GuardarTipoParadaValidador : AbstractValidator<GuardarTipoParadaRequest>
{
    public GuardarTipoParadaValidador()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CamposRequeridos).MaximumLength(1000);
    }
}
