using CleanCom.Domain.Access;
using FluentValidation;

namespace CleanCom.Application.Access;

public class SaveRoleValidator : AbstractValidator<SaveRoleRequest>
{
    public SaveRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60).WithName("Nombre");
        RuleFor(x => x.Name).Must(n => Roles.KeyFromName(n ?? "").Length > 0)
            .When(x => !string.IsNullOrWhiteSpace(x.Name))
            .WithMessage("El nombre del rol debe llevar al menos una letra o número.");
    }
}

public class SaveRolePermissionsValidator : AbstractValidator<SaveRolePermissionsRequest>
{
    public SaveRolePermissionsValidator()
    {
        RuleFor(x => x.Permissions).NotNull().WithName("Permisos");
        RuleForEach(x => x.Permissions).ChildRules(p =>
        {
            p.RuleFor(x => x.Screen).Must(Screens.Exists).WithMessage(x => $"La pantalla {x.Screen} no existe.");
            p.RuleFor(x => x.Level).IsInEnum().WithMessage("El nivel debe ser View o Edit.");
        });
        RuleFor(x => x.Permissions)
            .Must(ps => ps.Select(p => p.Screen).Distinct().Count() == ps.Count)
            .When(x => x.Permissions is not null)
            .WithMessage("Cada pantalla va una sola vez.");
    }
}
