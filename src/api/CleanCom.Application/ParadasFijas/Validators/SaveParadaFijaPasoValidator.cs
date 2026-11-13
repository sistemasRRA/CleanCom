using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using FluentValidation;

namespace CleanCom.Application.ParadasFijas;

/// <summary>A paso of a parada fija, or of a parada or viaje de un día.</summary>
public class SaveParadaFijaPasoValidator : AbstractValidator<SaveParadaFijaPasoRequest>
{
    public SaveParadaFijaPasoValidator()
    {
        RuleFor(x => x).Must(x => x.LugarId is not null || !string.IsNullOrWhiteSpace(x.Address))
            .WithMessage("Cada paso necesita un lugar del catálogo o una dirección.");
        RuleFor(x => x).Must(x => (x.Lat is null) == (x.Lng is null)).WithMessage("Indica latitud y longitud juntas.");
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90).WithName("Latitud").When(x => x.Lat is not null);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180).WithName("Longitud").When(x => x.Lng is not null);
        RuleFor(x => x.Address).MaximumLength(300).WithName("Dirección");
        RuleFor(x => x.Kind).Must(k => k is null || ParadaFija.AllowedKinds.Contains(k.Value))
            .WithMessage("Qué se hace del paso debe ser recolección, entrega, entrega y recolección o traslado.");
        RuleFor(x => x.Turno).Must(t => t is null || t != Turno.HoraEspecifica)
            .WithMessage("El turno del paso es mañana, tarde o cualquier hora; para una hora usa \"Antes de\".");
        RuleFor(x => x.MapsLink).MaximumLength(2000).WithName("Link de Google Maps o coordenadas");
        RuleFor(x => x.Activity).MaximumLength(200).WithName("Indicación");
        RuleFor(x => x.Notes).MaximumLength(500).WithName("Nota");
    }
}
