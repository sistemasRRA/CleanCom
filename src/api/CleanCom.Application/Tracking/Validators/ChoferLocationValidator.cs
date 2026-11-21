using FluentValidation;

namespace CleanCom.Application.Tracking;

public class ChoferLocationValidator : AbstractValidator<ChoferLocationsRequest>
{
    /// <summary>A day without signal at one reading every 30 seconds is less than this.</summary>
    public const int MaxReadings = 2000;

    public ChoferLocationValidator()
    {
        RuleFor(x => x.Readings).NotEmpty().WithName("Ubicaciones")
            .Must(r => r.Count <= MaxReadings).WithMessage($"Se pueden enviar hasta {MaxReadings} ubicaciones a la vez.");
        RuleForEach(x => x.Readings).ChildRules(r =>
        {
            r.RuleFor(x => x.Lat).InclusiveBetween(-90, 90).WithName("Latitud");
            r.RuleFor(x => x.Lng).InclusiveBetween(-180, 180).WithName("Longitud");
            r.RuleFor(x => x.AccuracyMeters).GreaterThanOrEqualTo(0).WithName("Precisión");
        });
    }
}
