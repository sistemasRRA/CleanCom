using FluentValidation;

namespace CleanCom.Application.Chofer;

public class StartShiftValidator : AbstractValidator<StartShiftRequest>
{
    public StartShiftValidator()
    {
        RuleFor(x => x.Km).InclusiveBetween(0, 9_999_999).WithName("Kilometraje inicial");
        RuleFor(x => x.LocalId).MaximumLength(64);
    }
}

public class EndShiftValidator : AbstractValidator<EndShiftRequest>
{
    public EndShiftValidator()
    {
        RuleFor(x => x.Km).InclusiveBetween(0, 9_999_999).WithName("Kilometraje final");
        RuleFor(x => x.Declared).NotNull().WithName("Cobros");
        RuleForEach(x => x.Declared).ChildRules(line =>
        {
            line.RuleFor(l => l.Method).IsInEnum().WithName("Forma de pago");
            line.RuleFor(l => l.Amount).InclusiveBetween(0, 1_000_000).WithName("Monto");
        });
        RuleFor(x => x.Declared)
            .Must(lines => lines.Select(l => l.Method).Distinct().Count() == lines.Count)
            .When(x => x.Declared is not null)
            .WithMessage("Declara cada forma de pago una sola vez.");
        RuleFor(x => x.Notes).MaximumLength(500).WithName("Notas");
        RuleFor(x => x.LocalId).MaximumLength(64);
    }
}

public class ReportFailureValidator : AbstractValidator<ReportFailureRequest>
{
    public ReportFailureValidator()
    {
        RuleFor(x => x.Description).NotEmpty().WithMessage("Describe la falla.").MaximumLength(900).WithName("Descripción");
        RuleFor(x => x.Severity).IsInEnum().WithName("Gravedad");
        RuleFor(x => x.LocalId).MaximumLength(64);
    }
}
