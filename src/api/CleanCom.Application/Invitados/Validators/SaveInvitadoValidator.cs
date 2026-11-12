using FluentValidation;

namespace CleanCom.Application.Invitados;

public class SaveInvitadoValidator : AbstractValidator<SaveInvitadoRequest>
{
    public SaveInvitadoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).WithName("Nombre");
        RuleFor(x => x.WhatsAppPhone).NotEmpty().WithName("WhatsApp");
        RuleFor(x => x.OtherPhone).MaximumLength(20).WithName("Otro teléfono");
        RuleFor(x => x.Email).EmailAddress().MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Email)).WithName("Correo");
        RuleFor(x => x.Type).IsInEnum().WithName("Tipo");
        RuleFor(x => x.HowFoundUs).MaximumLength(100).WithName("Cómo nos conoció");
        RuleFor(x => x.Campaign).MaximumLength(100).WithName("Campaña");
        RuleFor(x => x.Notes).MaximumLength(1000).WithName("Observaciones");
        RuleFor(x => x.Direccion!).SetValidator(new SaveDireccionValidator()).When(x => x.Direccion is not null);
    }
}
