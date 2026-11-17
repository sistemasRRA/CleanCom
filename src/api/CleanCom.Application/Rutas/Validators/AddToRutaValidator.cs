using CleanCom.Application.ParadasFijas;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using FluentValidation;

namespace CleanCom.Application.Rutas;

public class AddToRutaValidator : AbstractValidator<AddToRutaRequest>
{
    public AddToRutaValidator()
    {
        RuleFor(x => x.RutaId).GreaterThan(0).WithName("Ruta");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).WithName("Qué es");
        RuleFor(x => x.Category).IsInEnum().WithName("Tipo de parada");
        RuleFor(x => x.Kind).Must(k => ParadaFija.AllowedKinds.Contains(k))
            .WithMessage("Qué se hace debe ser recolección, entrega, entrega y recolección o traslado.");
        RuleFor(x => x.Turno).IsInEnum().NotEqual(Turno.HoraEspecifica)
            .WithMessage("Se pide por turno (mañana, tarde o cualquier hora); para una hora usa \"Antes de\" en el paso.");
        RuleFor(x => x.Placement).IsInEnum().WithName("Lugar en la ruta");
        RuleFor(x => x.Carried).MaximumLength(500).WithName("Insumos o carga");
        RuleFor(x => x.DriverNotes).MaximumLength(500).WithName("Notas para el chofer");
        RuleFor(x => x.Pasos).NotEmpty().WithMessage("Agrega al menos un paso (lugar o dirección).")
            .Must(p => p.Count <= SaveParadaFijaValidator.MaxPasos).WithMessage($"Máximo {SaveParadaFijaValidator.MaxPasos} pasos.");
        RuleForEach(x => x.Pasos).SetValidator(new SaveParadaFijaPasoValidator());
    }
}
