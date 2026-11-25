using CleanCom.Application.Common;
using CleanCom.Domain.Avisos;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Avisos;

/// <summary>Plantillas por evento (WhatsApp y metas): WhatsApp and push texts, Meta name and whether it is sent.</summary>
public class PlantillaService(ICleanComDbContext db, IValidator<PlantillaInput> validator)
{
    /// <summary>Sample values for the preview, by variable name.</summary>
    private static readonly IReadOnlyDictionary<string, string> Samples = new Dictionary<string, string>
    {
        ["nombre"] = "Ana",
        ["fecha"] = "jueves 26 de noviembre",
        ["turno"] = "en la mañana",
        ["número de pedido"] = "1024",
        ["prendas"] = "12 prendas",
    };

    public async Task<IReadOnlyList<PlantillaDto>> ListAsync(CancellationToken ct) =>
        (await db.Plantillas.AsNoTracking().ToListAsync(ct)).OrderBy(p => p.Evento).Select(ToDto).ToList();

    public async Task<PlantillaDto> SaveAsync(int id, SavePlantillaRequest r, CancellationToken ct)
    {
        var plantilla = await db.Plantillas.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException("La plantilla no existe.");
        await validator.ValidateAndThrowAsync(new PlantillaInput(plantilla.Evento, r), ct);
        plantilla.MetaName = r.MetaName.Trim();
        plantilla.WhatsAppText = r.WhatsAppText.Trim();
        plantilla.PushTitle = r.PushTitle.Trim();
        plantilla.PushText = r.PushText.Trim();
        plantilla.Active = r.Active;
        await db.SaveChangesAsync(ct);
        return ToDto(plantilla);
    }

    /// <summary>The texts (saved or not) filled in with sample data.</summary>
    public async Task<PlantillaPreviewDto> PreviewAsync(int id, SavePlantillaRequest r, CancellationToken ct)
    {
        var evento = await db.Plantillas.Where(p => p.Id == id).Select(p => (AvisoEvento?)p.Evento).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("La plantilla no existe.");
        var values = AvisoRules.Variables(evento).Select(v => Samples.GetValueOrDefault(v, v)).ToList();
        return new PlantillaPreviewDto(AvisoRules.Render(r.WhatsAppText, values), AvisoRules.Render(r.PushTitle, values),
            AvisoRules.Render(r.PushText, values));
    }

    private static PlantillaDto ToDto(Plantilla p) =>
        new(p.Id, p.Evento, AvisoRules.Name(p.Evento), AvisoRules.Variables(p.Evento), p.MetaName, p.WhatsAppText, p.PushTitle, p.PushText, p.Active);
}

/// <summary>A template with the event it belongs to (its variables decide which {{n}} can be used).</summary>
public record PlantillaInput(AvisoEvento Evento, SavePlantillaRequest Plantilla);

public class PlantillaValidator : AbstractValidator<PlantillaInput>
{
    public PlantillaValidator()
    {
        RuleFor(x => x.Plantilla.MetaName).NotEmpty().MaximumLength(100).Matches("^[a-z0-9_]+$")
            .WithMessage("El nombre en Meta va en minúsculas, números y guion bajo, como cleancom_cita_agendada.")
            .WithName("Nombre en Meta");
        RuleFor(x => x.Plantilla.WhatsAppText).NotEmpty().MaximumLength(1024).WithName("Texto de WhatsApp");
        RuleFor(x => x.Plantilla.PushTitle).NotEmpty().MaximumLength(65).WithName("Título del push");
        RuleFor(x => x.Plantilla.PushText).NotEmpty().MaximumLength(240).WithName("Texto del push");
        RuleFor(x => x).Custom((x, context) =>
        {
            var count = AvisoRules.Variables(x.Evento).Count;
            foreach (var text in new[] { x.Plantilla.WhatsAppText, x.Plantilla.PushTitle, x.Plantilla.PushText })
            {
                var wrong = AvisoRules.UsedVariables(text ?? "").Where(n => n < 1 || n > count).ToList();
                if (wrong.Count > 0)
                {
                    context.AddFailure($"Este aviso solo tiene {count} variable{(count == 1 ? "" : "s")}: {{{{{wrong[0]}}}}} no existe.");
                    return;
                }
            }
        });
    }
}
