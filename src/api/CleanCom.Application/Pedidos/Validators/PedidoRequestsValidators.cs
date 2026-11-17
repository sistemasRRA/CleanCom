using FluentValidation;

namespace CleanCom.Application.Pedidos;

public class MoveEtapaValidator : AbstractValidator<MoveEtapaRequest>
{
    public MoveEtapaValidator()
    {
        RuleFor(x => x.Etapa).IsInEnum().WithName("Etapa");
        RuleFor(x => x.Reason).MaximumLength(1000).WithName("Motivo");
    }
}

public class PedidoCommentValidator : AbstractValidator<PedidoCommentRequest>
{
    public PedidoCommentValidator()
    {
        RuleFor(x => x.Note).NotEmpty().MaximumLength(1000).WithName("Comentario");
    }
}
