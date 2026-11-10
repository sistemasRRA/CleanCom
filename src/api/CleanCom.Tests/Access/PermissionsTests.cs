using CleanCom.Application.Access;
using CleanCom.Domain.Access;

namespace CleanCom.Tests.Access;

public class PermissionsTests
{
    [Fact]
    public void Edit_includes_view()
    {
        string[] permissions = [Permissions.ToClaim(Screens.Operacion, AccessLevel.Edit)];
        Assert.True(Permissions.Allows(permissions, Screens.Operacion, AccessLevel.View));
        Assert.True(Permissions.Allows(permissions, Screens.Operacion, AccessLevel.Edit));
    }

    [Fact]
    public void View_is_not_enough_to_edit()
    {
        string[] permissions = [Permissions.ToClaim(Screens.Usuarios, AccessLevel.View)];
        Assert.False(Permissions.Allows(permissions, Screens.Usuarios, AccessLevel.Edit));
    }

    [Fact]
    public void Without_the_screen_permission_there_is_no_access()
    {
        string[] permissions = [Permissions.ToClaim(Screens.AppChofer, AccessLevel.Edit)];
        Assert.False(Permissions.Allows(permissions, Screens.Operacion, AccessLevel.View));
    }

    [Fact]
    public void Administracion_edits_every_office_screen_and_none_of_the_apps()
    {
        var admin = Permissions.Initial.Where(p => p.RoleId == Roles.AdministracionId).Select(p => p.Screen).ToHashSet();
        Assert.Contains(Screens.Usuarios, admin);
        Assert.Contains(Screens.Operacion, admin);
        Assert.DoesNotContain(Screens.AppChofer, admin);
        Assert.DoesNotContain(Screens.AppInvitado, admin);
        Assert.Equal(Screens.All.Count - 2, admin.Count);
    }

    [Fact]
    public void Chofer_and_invitado_only_have_their_app()
    {
        Assert.Equal([Screens.AppChofer], Permissions.Initial.Where(p => p.RoleId == Roles.ChoferId).Select(p => p.Screen));
        Assert.Equal([Screens.AppInvitado], Permissions.Initial.Where(p => p.RoleId == Roles.InvitadoId).Select(p => p.Screen));
    }
}

public class AccessValidatorsTests
{
    [Fact]
    public void Initial_password_needs_at_least_8_characters()
    {
        var r = new CreateUserRequest("Juan Rosales", "juan.rosales", null, null, Roles.Chofer, "1234567", null, true, null);
        var result = new CreateUserValidator().Validate(r);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserRequest.InitialPassword));
    }

    [Fact]
    public void A_chofer_with_a_short_user_name_is_valid()
    {
        var r = new CreateUserRequest("Juan Rosales", "juan.rosales", null, "5512345678", Roles.Chofer, "contraseña-1", null, true, null);
        Assert.True(new CreateUserValidator().Validate(r).IsValid);
    }

    [Fact]
    public void Only_a_chofer_is_linked_to_an_operador()
    {
        var r = new CreateUserRequest("Ana López", "ana", null, null, Roles.Administracion, "contraseña-1", 3, false, null);
        Assert.False(new CreateUserValidator().Validate(r).IsValid);
    }

    [Fact]
    public void Invitados_are_not_created_from_users()
    {
        var r = new CreateUserRequest("Invitado", "invitado1", null, null, Roles.Invitado, "contraseña-1", null, false, null);
        Assert.False(new CreateUserValidator().Validate(r).IsValid);
    }

    [Fact]
    public void Default_messages_use_the_spanish_field_name()
    {
        var r = new CreateUserRequest("", "juan.rosales", null, null, Roles.Chofer, "contraseña-1", null, true, null);
        var error = new CreateUserValidator().Validate(r).Errors.Single(e => e.PropertyName == nameof(CreateUserRequest.FullName));
        Assert.Contains("Nombre completo", error.ErrorMessage);
    }
}
