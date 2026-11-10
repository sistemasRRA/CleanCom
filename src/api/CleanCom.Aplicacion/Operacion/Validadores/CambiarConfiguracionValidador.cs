using CleanCom.Dominio.Operacion;
using FluentValidation;

namespace CleanCom.Aplicacion.Operacion;

public class CambiarConfiguracionValidador : AbstractValidator<CambiarConfiguracionRequest>
{
    public CambiarConfiguracionValidador()
    {
        RuleFor(x => x.Clave).NotEmpty().WithMessage("Indica la clave.")
            .Must(c => CatalogoConfiguracion.Buscar(c) is not null).WithMessage(x => $"La clave \"{x.Clave}\" no existe.");
        RuleFor(x => x.Valor).NotEmpty().WithMessage("Indica el valor.").MaximumLength(500);
        RuleFor(x => x.Nota).MaximumLength(500);
    }
}
