using FluentValidation;

namespace CleanCom.Aplicacion.Catalogos;

public class DiasVisitaValidador : AbstractValidator<DiasVisitaRequest>
{
    public DiasVisitaValidador()
    {
        RuleFor(x => x.Dias).NotNull();
        RuleForEach(x => x.Dias).IsInEnum();
    }
}
