using FluentValidation;

namespace CleanCom.Application.ParadasFijas;

public class SaveParadaFijaExceptionValidator : AbstractValidator<SaveParadaFijaExceptionRequest>
{
    public SaveParadaFijaExceptionValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(200).WithName("Motivo");
    }
}
