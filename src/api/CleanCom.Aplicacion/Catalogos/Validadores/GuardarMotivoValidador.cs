using FluentValidation;

namespace CleanCom.Aplicacion.Catalogos;

public class GuardarMotivoValidador : AbstractValidator<GuardarMotivoRequest>
{
    public GuardarMotivoValidador()
    {
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Texto).NotEmpty().MaximumLength(200);
    }
}
