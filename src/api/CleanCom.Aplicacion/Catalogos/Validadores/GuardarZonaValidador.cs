using FluentValidation;

namespace CleanCom.Aplicacion.Catalogos;

public class GuardarZonaValidador : AbstractValidator<GuardarZonaRequest>
{
    public GuardarZonaValidador()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(20).Matches("^[A-Z0-9]+$").WithMessage("El código va en mayúsculas, sin espacios (por ejemplo K3 o K1EXT).");
        RuleFor(x => x.RutaId).GreaterThan(0);
        RuleFor(x => x.Poligono!.Count).GreaterThanOrEqualTo(3).When(x => x.Poligono is { Count: > 0 })
            .WithMessage("El polígono necesita al menos 3 vértices.");
    }
}
