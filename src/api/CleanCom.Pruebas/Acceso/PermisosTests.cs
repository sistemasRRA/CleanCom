using CleanCom.Aplicacion.Acceso;
using CleanCom.Dominio.Acceso;

namespace CleanCom.Pruebas.Acceso;

public class PermisosTests
{
    [Fact]
    public void Editar_incluye_ver()
    {
        string[] permisos = [Permisos.AClaim(Pantallas.Operacion, NivelAcceso.Editar)];

        Assert.True(Permisos.Permite(permisos, Pantallas.Operacion, NivelAcceso.Ver));
        Assert.True(Permisos.Permite(permisos, Pantallas.Operacion, NivelAcceso.Editar));
    }

    [Fact]
    public void Ver_no_alcanza_para_editar()
    {
        string[] permisos = [Permisos.AClaim(Pantallas.Usuarios, NivelAcceso.Ver)];

        Assert.False(Permisos.Permite(permisos, Pantallas.Usuarios, NivelAcceso.Editar));
    }

    [Fact]
    public void Sin_permiso_de_la_pantalla_no_hay_acceso()
    {
        string[] permisos = [Permisos.AClaim(Pantallas.AppChofer, NivelAcceso.Editar)];

        Assert.False(Permisos.Permite(permisos, Pantallas.Operacion, NivelAcceso.Ver));
    }

    [Fact]
    public void Administracion_edita_todas_las_pantallas_de_oficina_y_no_las_apps()
    {
        var admin = Permisos.Iniciales.Where(p => p.RolId == Roles.IdAdministracion).Select(p => p.Pantalla).ToHashSet();

        Assert.Contains(Pantallas.Usuarios, admin);
        Assert.Contains(Pantallas.Operacion, admin);
        Assert.DoesNotContain(Pantallas.AppChofer, admin);
        Assert.DoesNotContain(Pantallas.AppInvitado, admin);
        Assert.Equal(Pantallas.Todas.Count - 2, admin.Count);
    }

    [Fact]
    public void Chofer_e_invitado_solo_tienen_su_app()
    {
        Assert.Equal([Pantallas.AppChofer], Permisos.Iniciales.Where(p => p.RolId == Roles.IdChofer).Select(p => p.Pantalla));
        Assert.Equal([Pantallas.AppInvitado], Permisos.Iniciales.Where(p => p.RolId == Roles.IdInvitado).Select(p => p.Pantalla));
    }
}

public class ValidadoresAccesoTests
{
    [Fact]
    public void La_contrasena_inicial_pide_minimo_8_caracteres()
    {
        var r = new CrearUsuarioRequest("Juan Rosales", "juan.rosales", null, null, Roles.Chofer, "1234567", null, true, null);

        var resultado = new CrearUsuarioValidador().Validate(r);

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CrearUsuarioRequest.ContrasenaInicial));
    }

    [Fact]
    public void Un_chofer_con_usuario_corto_es_valido()
    {
        var r = new CrearUsuarioRequest("Juan Rosales", "juan.rosales", null, "5512345678", Roles.Chofer, "contraseña-1", null, true, null);

        Assert.True(new CrearUsuarioValidador().Validate(r).IsValid);
    }

    [Fact]
    public void Solo_un_chofer_se_liga_a_un_operador()
    {
        var r = new CrearUsuarioRequest("Ana López", "ana", null, null, Roles.Administracion, "contraseña-1", 3, false, null);

        Assert.False(new CrearUsuarioValidador().Validate(r).IsValid);
    }

    [Fact]
    public void Los_invitados_no_se_crean_desde_usuarios()
    {
        var r = new CrearUsuarioRequest("Invitado", "invitado1", null, null, Roles.Invitado, "contraseña-1", null, false, null);

        Assert.False(new CrearUsuarioValidador().Validate(r).IsValid);
    }
}
