using FluentValidation;

namespace CleanCom.Application.Catalogs;

public class SaveZonaValidator : AbstractValidator<SaveZonaRequest>
{
    public SaveZonaValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).WithName("Código").Matches("^[A-Z0-9]+$")
            .WithMessage("El código va en mayúsculas, sin espacios (por ejemplo K3 o K1EXT).");
        RuleFor(x => x.RutaId).GreaterThan(0).WithName("Ruta");
        RuleFor(x => x.Polygon!.Count).GreaterThanOrEqualTo(3).When(x => x.Polygon is { Count: > 0 })
            .WithMessage("El polígono necesita al menos 3 vértices.");
    }
}
