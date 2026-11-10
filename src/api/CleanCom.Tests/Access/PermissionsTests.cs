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
    public void Administracion_edits_every_office_screen_except_roles_and_none_of_the_apps()
    {
        var admin = Permissions.Initial.Where(p => p.RoleId == Roles.AdministracionId).Select(p => p.Screen).ToHashSet();
        Assert.Contains(Screens.Usuarios, admin);
        Assert.Contains(Screens.Operacion, admin);
        Assert.DoesNotContain(Screens.AppChofer, admin);
        Assert.DoesNotContain(Screens.AppInvitado, admin);
        Assert.DoesNotContain(Screens.RolesYPermisos, admin);
        Assert.Equal(Screens.All.Count - 3, admin.Count);
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

public class RolesTests
{
    [Theory]
    [InlineData("Logística", "Logistica")]
    [InlineData("Atención a clientes", "AtencionAClientes")]
    [InlineData("  ventas  CDMX ", "VentasCdmx")]
    [InlineData("Ruta #2", "Ruta2")]
    [InlineData(" ¿? ", "")]
    public void Key_from_name_drops_accents_spaces_and_symbols(string name, string key) =>
        Assert.Equal(key, Roles.KeyFromName(name));

    [Fact]
    public void System_roles_are_the_office_ones_plus_chofer_and_invitado()
    {
        Assert.Equal(
            [Roles.Administracion, Roles.Chofer, Roles.Invitado, Roles.Gerencia, Roles.Logistica, Roles.Telemarketing, Roles.Planta, Roles.SuperUser],
            Roles.System.Select(r => r.Key));
        Assert.Equal("Logística", Roles.System.Single(r => r.Key == Roles.Logistica).Name);
        Assert.Equal(Roles.System.Count, Roles.System.Select(r => r.Id).Distinct().Count());
    }

    [Fact]
    public void Only_superuser_has_locked_permissions_and_invitado_is_not_assigned_in_the_office()
    {
        Assert.Equal([Roles.SuperUser], Roles.System.Where(r => Roles.HasLockedPermissions(r.Key)).Select(r => r.Key));
        Assert.Equal([Roles.Invitado], Roles.System.Where(r => !Roles.IsAssignableInOffice(r.Key)).Select(r => r.Key));
    }

    [Fact]
    public void Initial_permissions_point_to_existing_screens_once_per_role()
    {
        Assert.All(Permissions.Initial, p => Assert.True(Screens.Exists(p.Screen), p.Screen));
        Assert.Equal(Permissions.Initial.Count, Permissions.Initial.Select(p => (p.RoleId, p.Screen)).Distinct().Count());
    }

    [Fact]
    public void Only_administracion_starts_with_access_to_usuarios()
    {
        var withUsuarios = Permissions.Initial.Where(p => p.Screen == Screens.Usuarios).Select(p => p.RoleId).Distinct();
        Assert.Equal([Roles.AdministracionId], withUsuarios);
    }

    [Fact]
    public void Gerencia_starts_viewing_only()
    {
        var gerencia = Permissions.Initial.Where(p => p.RoleId == Roles.GerenciaId).ToList();
        Assert.NotEmpty(gerencia);
        Assert.All(gerencia, p => Assert.Equal(AccessLevel.View, p.Level));
        Assert.Contains(gerencia, p => p.Screen == Screens.Kpis);
    }

    [Fact]
    public void Office_roles_never_start_with_the_apps()
    {
        int[] office = [Roles.GerenciaId, Roles.LogisticaId, Roles.TelemarketingId, Roles.PlantaId];
        Assert.DoesNotContain(Permissions.Initial, p => office.Contains(p.RoleId) && (p.Screen == Screens.AppChofer || p.Screen == Screens.AppInvitado));
    }

    [Fact]
    public void Every_screen_has_a_menu_group()
    {
        Assert.All(Screens.All, s => Assert.False(string.IsNullOrWhiteSpace(s.Group), s.Key));
    }
}

public class RoleValidatorsTests
{
    [Fact]
    public void A_role_name_needs_a_letter_or_number()
    {
        Assert.False(new SaveRoleValidator().Validate(new SaveRoleRequest(" ¿? ")).IsValid);
        Assert.False(new SaveRoleValidator().Validate(new SaveRoleRequest("")).IsValid);
        Assert.True(new SaveRoleValidator().Validate(new SaveRoleRequest("Atención a clientes")).IsValid);
    }

    [Fact]
    public void Permissions_must_be_existing_screens_without_repeats()
    {
        var validator = new SaveRolePermissionsValidator();
        Assert.True(validator.Validate(new SaveRolePermissionsRequest(
            [new(Screens.Rutas, AccessLevel.Edit), new(Screens.Kpis, AccessLevel.View)])).IsValid);
        Assert.False(validator.Validate(new SaveRolePermissionsRequest([new("no-existe", AccessLevel.View)])).IsValid);
        Assert.False(validator.Validate(new SaveRolePermissionsRequest(
            [new(Screens.Rutas, AccessLevel.Edit), new(Screens.Rutas, AccessLevel.View)])).IsValid);
        Assert.False(validator.Validate(new SaveRolePermissionsRequest([new(Screens.Rutas, (AccessLevel)9)])).IsValid);
    }

    [Fact]
    public void An_empty_list_removes_every_permission()
    {
        Assert.True(new SaveRolePermissionsValidator().Validate(new SaveRolePermissionsRequest([])).IsValid);
    }

    [Theory]
    [InlineData(Roles.Gerencia)]
    [InlineData(Roles.Logistica)]
    [InlineData(Roles.Telemarketing)]
    [InlineData(Roles.Planta)]
    [InlineData("RolNuevo")]
    public void Users_can_get_any_office_role(string role)
    {
        var r = new CreateUserRequest("Ana López", "ana", null, null, role, "contraseña-1", null, false, null);
        Assert.True(new CreateUserValidator().Validate(r).IsValid);
        Assert.True(new UpdateUserValidator().Validate(new UpdateUserRequest("Ana López", null, null, role, true, null)).IsValid);
    }
}

public class SuperUserTests
{
    [Fact]
    public void Superuser_always_has_every_office_screen_including_roles_y_permisos()
    {
        var full = Permissions.FullOffice.ToList();
        Assert.Equal(Screens.Office.Count(), full.Count);
        Assert.Contains((Screens.RolesYPermisos, AccessLevel.Edit), full);
        Assert.All(full, p => Assert.Equal(AccessLevel.Edit, p.Level));
        Assert.DoesNotContain(full, p => p.Screen is Screens.AppChofer or Screens.AppInvitado);
    }

    [Fact]
    public void Nobody_starts_with_roles_y_permisos_except_superuser()
    {
        Assert.DoesNotContain(Permissions.Initial, p => p.Screen == Screens.RolesYPermisos);
    }

    [Fact]
    public void Superuser_needs_no_rows_in_the_database()
    {
        Assert.DoesNotContain(Permissions.Initial, p => p.RoleId == Roles.SuperUserId);
    }

    [Fact]
    public void Roles_y_permisos_is_an_office_screen_in_the_administracion_group()
    {
        var screen = Screens.All.Single(s => s.Key == Screens.RolesYPermisos);
        Assert.Equal("Administración", screen.Group);
        Assert.Contains(screen, Screens.Office);
    }
}
