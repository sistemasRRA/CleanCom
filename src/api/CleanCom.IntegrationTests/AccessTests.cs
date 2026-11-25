using System.Net;
using CleanCom.IntegrationTests.Infrastructure;

namespace CleanCom.IntegrationTests;

/// <summary>Sign-in and permissions (docs/pantallas/oficina/usuarios.md): the API checks every request, not the screens.</summary>
[Collection("api")]
public class AccessTests(ApiFactory api)
{
    [Fact]
    public async Task Wrong_password_is_rejected_with_a_message_in_Spanish()
    {
        var response = await api.Anonymous().PostJsonAsync("/auth/login", new { userName = ApiFactory.AdminUser, password = "No.Es.La.Clave1" });

        var problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Contains("contraseña", problem.GetProperty("detail").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Office_endpoints_need_a_session()
    {
        var response = await api.Anonymous().GetAsync("/pedidos/tablero");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Initial_password_must_be_changed_and_then_the_session_works()
    {
        var admin = await api.AdminAsync();

        var rutas = await admin.GetOkAsync("/rutas");

        Assert.Equal(System.Text.Json.JsonValueKind.Array, rutas.ValueKind);
    }

    [Fact]
    public async Task A_role_that_only_views_Pedidos_cannot_move_them_nor_see_Usuarios()
    {
        var admin = await api.AdminAsync();
        await admin.PostOkAsync("/usuarios", new
        {
            fullName = "Pedro Planta Ruiz",
            userName = "planta.pruebas",
            email = (string?)null,
            phone = "5522223333",
            role = "Planta",
            initialPassword = "Planta.Inicial2026",
            operadorId = (int?)null,
            createOperador = false,
            usualRutaId = (int?)null,
        });
        var planta = api.WithToken(await api.SignInOfficeAsync("planta.pruebas", "Planta.Inicial2026", "Planta.Pruebas2026"));

        var board = await planta.GetAsync("/pedidos/tablero");
        var move = await planta.PostJsonAsync("/pedidos/1/etapa", new { etapa = "Recolectado", reason = (string?)null });
        var users = await planta.GetAsync("/usuarios");

        Assert.Equal(HttpStatusCode.OK, board.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, move.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, users.StatusCode);
    }

    [Fact]
    public async Task The_invitado_app_cannot_use_office_endpoints()
    {
        var invitado = await api.InvitadoAsync("5530000001");

        var avisos = await invitado.GetAsync("/avisos");
        var me = await invitado.GetAsync("/yo");

        Assert.Equal(HttpStatusCode.Forbidden, avisos.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }
}
