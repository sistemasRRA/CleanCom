using FluentValidation;

namespace CleanCom.Application.Pedidos;

public class CaptureTicketValidator : AbstractValidator<CaptureTicketRequest>
{
    public CaptureTicketValidator()
    {
        RuleFor(x => x.Folio).NotEmpty().MaximumLength(40).WithName("Folio");
        RuleFor(x => x.PedidoId).GreaterThan(0).WithName("Pedido");
        RuleFor(x => x.LineaServicioId).GreaterThan(0).WithName("Línea de servicio");
        RuleFor(x => x.Garments).InclusiveBetween(1, 500).WithName("Prendas");
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount is not null).WithName("Monto");
    }
}

public class PlantaCommentValidator : AbstractValidator<PlantaCommentRequest>
{
    public PlantaCommentValidator()
    {
        RuleFor(x => x.PedidoId).GreaterThan(0).WithName("Pedido");
        RuleFor(x => x.Note).MaximumLength(1000).WithName("Comentario");
        RuleFor(x => x).Must(x => x.ReasonId is not null || !string.IsNullOrWhiteSpace(x.Note))
            .WithMessage("Elige un motivo o escribe por qué se atrasa.");
    }
}
