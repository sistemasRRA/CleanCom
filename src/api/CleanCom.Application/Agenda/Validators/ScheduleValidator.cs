using CleanCom.Domain.Pedidos;
using FluentValidation;

namespace CleanCom.Application.Agenda;

public class ScheduleValidator : AbstractValidator<ScheduleRequest>
{
    public ScheduleValidator()
    {
        RuleFor(x => x.InvitadoId).GreaterThan(0).WithName("Invitado");
        RuleFor(x => x.DireccionId).GreaterThan(0).WithName("Dirección");
        RuleFor(x => x.Turno).IsInEnum().WithName("Turno");
        RuleFor(x => x.SpecificTime).NotNull().When(x => x.Turno == Turno.HoraEspecifica)
            .WithMessage("Indica la hora específica.");
        RuleFor(x => x.SpecificTime).Null().When(x => x.Turno != Turno.HoraEspecifica)
            .WithMessage("La hora específica solo va con el turno \"Hora específica\".");
        RuleFor(x => x.GarmentsApprox).InclusiveBetween(0, 500).When(x => x.GarmentsApprox is not null).WithName("Prendas aproximadas");
        RuleFor(x => x.DriverNotes).MaximumLength(500).WithName("Notas para el chofer");
        RuleForEach(x => x.LineaServicioIds).GreaterThan(0).WithName("Línea de servicio");
    }
}
