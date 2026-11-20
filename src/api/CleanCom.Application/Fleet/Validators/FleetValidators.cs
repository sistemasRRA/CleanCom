using CleanCom.Domain.Fleet;
using FluentValidation;

namespace CleanCom.Application.Fleet;

public class SaveUnidadValidator : AbstractValidator<SaveUnidadRequest>
{
    public SaveUnidadValidator()
    {
        RuleFor(x => x.Plate).NotEmpty().MaximumLength(15).WithName("Placa");
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(60).WithName("Marca");
        RuleFor(x => x.Model).NotEmpty().MaximumLength(60).WithName("Modelo");
        RuleFor(x => x.Year).InclusiveBetween(1980, 2100).WithName("Año");
        RuleFor(x => x.GarmentCapacity).GreaterThan(0).WithName("Capacidad de prendas");
        RuleFor(x => x.Km).GreaterThanOrEqualTo(0).WithName("Kilometraje");
        RuleFor(x => x.ServiceEveryKm).GreaterThan(0).WithName("Servicio cada km");
        RuleFor(x => x.ServiceEveryMonths).InclusiveBetween(1, 60).WithName("Servicio cada meses");
        RuleFor(x => x.State).IsInEnum().WithName("Estado");
        RuleFor(x => x.Notes).MaximumLength(1000).WithName("Notas");
    }
}

public class SaveUnidadEventValidator : AbstractValidator<SaveUnidadEventRequest>
{
    public SaveUnidadEventValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().WithName("Tipo");
        RuleFor(x => x.Km).GreaterThanOrEqualTo(0).WithName("Kilometraje");
        RuleFor(x => x.Km).NotNull().When(x => x.Kind == UnidadEventKind.Kilometraje).WithName("Kilometraje");
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).WithName("Monto");
        RuleFor(x => x.Description).MaximumLength(1000).WithName("Descripción");
        RuleFor(x => x.Description).NotEmpty().When(x => x.Kind is UnidadEventKind.Falla or UnidadEventKind.Reparacion).WithName("Descripción");
        RuleFor(x => x.Severity).NotNull().When(x => x.Kind == UnidadEventKind.Falla).WithName("Gravedad")
            .WithMessage("Indica la gravedad de la falla.");
        RuleFor(x => x.Severity).Null().When(x => x.Kind != UnidadEventKind.Falla)
            .WithMessage("La gravedad solo aplica a una falla.");
        RuleFor(x => x.RecurringPaymentId).Null().When(x => x.Kind != UnidadEventKind.Pago)
            .WithMessage("Solo un pago puede cubrir un pago recurrente.");
    }
}

public class SaveRecurringPaymentValidator : AbstractValidator<SaveRecurringPaymentRequest>
{
    public SaveRecurringPaymentValidator()
    {
        RuleFor(x => x.Concept).NotEmpty().MaximumLength(150).WithName("Concepto");
        RuleFor(x => x.Amount).GreaterThan(0).WithName("Monto");
        RuleFor(x => x.Frequency).IsInEnum().WithName("Periodicidad");
    }
}

public class FleetAlertRuleValidator : AbstractValidator<FleetAlertRuleDto>
{
    private static readonly string[] KnownChannels = ["Correo", "Web", "Push"];

    public FleetAlertRuleValidator()
    {
        RuleFor(x => x.Event).IsInEnum().WithName("Evento");
        RuleFor(x => x.DaysBefore).NotEmpty().MaximumLength(100).WithName("Días antes")
            .Must(d => FleetRules.ParseDays(d).Count == d.Split(',', StringSplitOptions.RemoveEmptyEntries).Length)
            .WithMessage("Los días deben ser números separados por coma, por ejemplo 30,7,0.");
        RuleFor(x => x.Recipients).MaximumLength(500).WithName("Destinatarios");
        RuleFor(x => x.Channels).MaximumLength(100).WithName("Canales")
            .Must(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).All(KnownChannels.Contains))
            .WithMessage("Los canales pueden ser Correo, Web o Push, separados por coma.");
    }
}
