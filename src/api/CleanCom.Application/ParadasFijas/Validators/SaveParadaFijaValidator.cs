using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using FluentValidation;

namespace CleanCom.Application.ParadasFijas;

public class SaveParadaFijaValidator : AbstractValidator<SaveParadaFijaRequest>
{
    public const int MaxPasos = 10;

    public SaveParadaFijaValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).WithName("Nombre");
        RuleFor(x => x.Kind).Must(k => ParadaFija.AllowedKinds.Contains(k))
            .WithMessage("El tipo de parada debe ser convenio, insumos, traspaso, recolección, entrega o entrega y recolección.");
        RuleFor(x => x.RutaId).GreaterThan(0).WithName("Ruta");
        RuleFor(x => x.Turno).IsInEnum().NotEqual(Turno.HoraEspecifica)
            .WithMessage("Una parada fija se pide por turno (mañana, tarde o cualquier hora).");
        RuleFor(x => x.Placement).IsInEnum().WithName("Lugar en la ruta");
        RuleFor(x => x.Days).NotEmpty().WithMessage("Elige al menos un día.");
        RuleForEach(x => x.Days).IsInEnum().WithName("Día");
        RuleFor(x => x.Carried).MaximumLength(500).WithName("Insumos o carga");
        RuleFor(x => x.SiteContact).MaximumLength(150).WithName("Contacto en sitio");
        RuleFor(x => x.SitePhone).MaximumLength(20).WithName("Teléfono");
        RuleFor(x => x.GarmentsApprox).InclusiveBetween(0, 1000).WithName("Prendas aproximadas");
        RuleFor(x => x.DriverNotes).MaximumLength(500).WithName("Notas para el chofer");
        RuleFor(x => x).Must(x => x.ValidFrom is null || x.ValidUntil is null || x.ValidUntil >= x.ValidFrom)
            .WithMessage("La vigencia termina antes de empezar.");
        RuleFor(x => x.Pasos).NotEmpty().WithMessage("Agrega al menos un paso (lugar o dirección).")
            .Must(p => p.Count <= MaxPasos).WithMessage($"Máximo {MaxPasos} pasos.");
        RuleForEach(x => x.Pasos).ChildRules(p =>
        {
            p.RuleFor(x => x).Must(x => x.LugarId is not null || !string.IsNullOrWhiteSpace(x.Address))
                .WithMessage("Cada paso necesita un lugar del catálogo o una dirección.");
            p.RuleFor(x => x).Must(x => (x.Lat is null) == (x.Lng is null)).WithMessage("Indica latitud y longitud juntas.");
            p.RuleFor(x => x.Lat).InclusiveBetween(-90, 90).WithName("Latitud").When(x => x.Lat is not null);
            p.RuleFor(x => x.Lng).InclusiveBetween(-180, 180).WithName("Longitud").When(x => x.Lng is not null);
            p.RuleFor(x => x.Address).MaximumLength(300).WithName("Dirección");
            p.RuleFor(x => x.Activity).MaximumLength(200).WithName("Qué se hace");
            p.RuleFor(x => x.Notes).MaximumLength(500).WithName("Nota");
        });
    }
}
