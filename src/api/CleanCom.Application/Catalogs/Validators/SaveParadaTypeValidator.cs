using FluentValidation;

namespace CleanCom.Application.Catalogs;

public class SaveParadaTypeValidator : AbstractValidator<SaveParadaTypeRequest>
{
    public SaveParadaTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithName("Nombre");
        RuleFor(x => x.RequiredFields).MaximumLength(1000).WithName("Campos requeridos");
    }
}
