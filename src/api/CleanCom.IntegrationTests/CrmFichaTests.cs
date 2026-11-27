using System.Net;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
using CleanCom.Domain.Time;
using CleanCom.Infrastructure.Persistence;
using CleanCom.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCom.IntegrationTests;

/// <summary>The CRM ficha (docs/pantallas/oficina/clientes-crm.md): value, history, duplicates and the app account.</summary>
[Collection("api")]
public class CrmFichaTests(ApiFactory api)
{
    [Fact]
    public async Task A_duplicate_by_phone_is_merged_with_its_pedidos_and_the_history_shows_them()
    {
        var admin = await api.AdminAsync();
        var (mainId, duplicateId, pedidoId) = await TwinsAsync();

        var duplicates = await admin.GetOkAsync($"/invitados/{mainId}/duplicados");
        var found = Assert.Single(duplicates.EnumerateArray());
        Assert.Equal(duplicateId, found.GetProperty("id").GetInt32());
        Assert.Equal(1, found.GetProperty("pedidos").GetInt32());

        var ficha = await admin.PostOkAsync($"/invitados/{mainId}/fusionar", new { duplicadoId = duplicateId });
        Assert.Equal("5532000002", ficha.GetProperty("invitado").GetProperty("otherPhone").GetString());
        Assert.Equal("gemelo@correo.mx", ficha.GetProperty("invitado").GetProperty("email").GetString());
        Assert.Contains(ficha.GetProperty("pedidos").EnumerateArray(), p => p.GetProperty("id").GetInt32() == pedidoId);
        var value = ficha.GetProperty("value");
        Assert.Equal(420m, value.GetProperty("totalSpent").GetDecimal());
        Assert.Equal(1, value.GetProperty("tickets").GetInt32());
        Assert.Equal("SinApp", ficha.GetProperty("account").GetProperty("state").GetString());
        Assert.Empty((await admin.GetOkAsync($"/invitados/{mainId}/duplicados")).EnumerateArray());
        var gone = await admin.GetAsync($"/invitados/{duplicateId}/ficha");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);

        var history = await admin.GetOkAsync($"/invitados/{mainId}/historia");
        Assert.Contains(history.EnumerateArray(), h => h.GetProperty("kind").GetString() == "Pedido" && h.GetProperty("pedidoId").GetInt32() == pedidoId);
        Assert.Contains(history.EnumerateArray(), h => h.GetProperty("kind").GetString() == "Ticket" && h.GetProperty("amount").GetDecimal() == 420m);

        // Invitation by WhatsApp: the text with the first name; the account becomes "invitado".
        var invitation = await admin.PostOkAsync($"/invitados/{mainId}/invitar-app");
        Assert.StartsWith("Hola Gemela,", invitation.GetProperty("text").GetString());
        Assert.StartsWith("https://wa.me/525532000001?text=", invitation.GetProperty("whatsAppUrl").GetString());
        var invited = await admin.GetOkAsync($"/invitados/{mainId}/ficha");
        Assert.Equal("Invitado", invited.GetProperty("account").GetProperty("state").GetString());
        var list = await admin.GetOkAsync("/invitados?cuenta=Invitado&tipo=ClienteFinal&porPagina=200");
        Assert.Contains(list.GetProperty("rows").EnumerateArray(), r => r.GetProperty("id").GetInt32() == mainId);
        // Without an account there is nothing to block.
        var block = await admin.PostJsonAsync($"/invitados/{mainId}/bloquear-app", new { blocked = true });
        await block.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_app_account_is_blocked_and_its_sessions_end()
    {
        var admin = await api.AdminAsync();
        int id;
        // An invitado with an app account and an open session, as the app leaves it (without asking a code: the
        // sign-in endpoints have a limit per minute that the other tests already use).
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CleanComDbContext>();
            var user = new CleanCom.Infrastructure.Identity.User
            {
                UserName = "inv5532000009", FullName = "Con app", PhoneNumber = "5532000009", MustChangePassword = false, CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            var invitado = new Invitado { Name = "Con app", WhatsAppPhone = "5532000009", UserId = user.Id };
            db.Invitados.Add(invitado);
            db.RefreshTokens.Add(new CleanCom.Infrastructure.Identity.RefreshToken
            {
                UserId = user.Id, TokenHash = "prueba-bloqueo", CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            });
            await db.SaveChangesAsync();
            id = invitado.Id;
        }
        var ficha = await admin.PostOkAsync($"/invitados/{id}/bloquear-app", new { blocked = true });
        Assert.Equal("Bloqueada", ficha.GetProperty("account").GetProperty("state").GetString());
        // Blocking ends the open sessions: the app cannot renew its session (sign-in already refuses a blocked user).
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CleanComDbContext>();
            var userId = await db.Invitados.Where(i => i.Id == id).Select(i => i.UserId!.Value).SingleAsync();
            Assert.False(await db.RefreshTokens.AnyAsync(t => t.UserId == userId && t.RevokedAt == null));
        }

        ficha = await admin.PostOkAsync($"/invitados/{id}/bloquear-app", new { blocked = false });
        Assert.Equal("Activa", ficha.GetProperty("account").GetProperty("state").GetString());
    }

    /// <summary>
    /// The same person captured twice: "Gemela López" (5532000001, other phone 5532000002) and "Gemela" (5532000002,
    /// with email and a pedido with a $420 ticket). Written straight to the database: capture rejects a repeated phone.
    /// </summary>
    private async Task<(int Main, int Duplicate, int Pedido)> TwinsAsync()
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CleanComDbContext>();
        var main = new Invitado { Name = "Gemela López", WhatsAppPhone = "5532000001", OtherPhone = "5532000002" };
        var duplicate = new Invitado { Name = "Gemela", WhatsAppPhone = "5532000002", Email = "gemelo@correo.mx" };
        db.Invitados.AddRange(main, duplicate);
        var date = scope.ServiceProvider.GetRequiredService<IBusinessClock>().Today.AddDays(-3);
        var pedido = new Pedido
        {
            Invitado = duplicate,
            Address = new Address { Invitado = duplicate, Street = "Calle de prueba 2", PostalCode = "03340" },
            Etapa = PedidoEtapa.Entregado,
            RegisteredAt = BusinessTime.ToUtc(date, new TimeOnly(9, 0)),
            RecoleccionDate = date,
        };
        db.Pedidos.Add(pedido);
        db.Tickets.Add(new Ticket { Folio = "GEM-1", Pedido = pedido, LineaServicioId = (await db.LineasServicio.FirstAsync()).Id, Garments = 4, Amount = 420 });
        await db.SaveChangesAsync();
        return (main.Id, duplicate.Id, pedido.Id);
    }
}
