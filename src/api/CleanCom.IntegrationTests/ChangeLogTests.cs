using System.Net;
using System.Text.Json;
using CleanCom.IntegrationTests.Infrastructure;

namespace CleanCom.IntegrationTests;

/// <summary>Bitácora de cambios (docs/03-reglas-de-negocio.md): who changed what in the key records.</summary>
[Collection("api")]
public class ChangeLogTests(ApiFactory api)
{
    [Fact]
    public async Task Capturing_and_editing_an_invitado_is_in_their_bitacora()
    {
        var admin = await api.AdminAsync();
        var address = new
        {
            label = "Casa", street = "Calle Bitácora", exteriorNumber = "12", interiorNumber = (string?)null, postalCode = "03340",
            neighborhood = "Portales", borough = "Benito Juárez", references = (string?)null, lat = (double?)null, lng = (double?)null, zonaId = (int?)null,
        };
        var created = await admin.PostOkAsync("/invitados", new
        {
            name = "Bitácora Prueba", whatsAppPhone = "5532000031", otherPhone = (string?)null, email = (string?)null, type = "ClienteFinal",
            howFoundUs = (string?)null, campaign = (string?)null, notes = (string?)null, address, receivesWhatsApp = true,
        });
        var id = created.GetProperty("id").GetInt32();
        await admin.PutOkAsync($"/invitados/{id}", new
        {
            name = "Bitácora Prueba", whatsAppPhone = "5532000031", otherPhone = (string?)null, email = (string?)null, type = "ClienteFinal",
            howFoundUs = (string?)null, campaign = (string?)null, notes = "Portón verde", receivesWhatsApp = true,
        });

        var log = await admin.GetOkAsync($"/invitados/{id}/bitacora");
        var rows = log.GetProperty("rows").EnumerateArray().ToList();
        // Newest first: the change of observaciones, then the alta of the invitado and of their address.
        var change = rows[0];
        Assert.Equal("Cambio", change.GetProperty("action").GetString());
        Assert.False(string.IsNullOrEmpty(change.GetProperty("userName").GetString()));
        var notes = Assert.Single(change.GetProperty("changes").EnumerateArray());
        Assert.Equal("Observaciones", notes.GetProperty("label").GetString());
        Assert.Equal(JsonValueKind.Null, notes.GetProperty("before").ValueKind);
        Assert.Equal("Portón verde", notes.GetProperty("after").GetString());
        Assert.Contains(rows, r => r.GetProperty("entityType").GetString() == "Invitado" && r.GetProperty("action").GetString() == "Alta");
        var alta = Assert.Single(rows, r => r.GetProperty("entityType").GetString() == "Address");
        Assert.Equal("Dirección", alta.GetProperty("entityLabel").GetString());
        Assert.Contains(alta.GetProperty("changes").EnumerateArray(), c => c.GetProperty("label").GetString() == "Calle" && c.GetProperty("after").GetString() == "Calle Bitácora");

        // The general bitácora is for SuperUser and Administración; the app never sees it.
        var general = await admin.GetOkAsync($"/bitacora?entidad=Invitado&id={id}");
        Assert.Equal(rows.Count, general.GetProperty("rowCount").GetInt32());
        var invitado = await api.InvitadoAsync("5530000106");
        Assert.Equal(HttpStatusCode.Forbidden, (await invitado.GetAsync("/bitacora")).StatusCode);
    }

    [Fact]
    public async Task Saving_permissions_logs_only_what_changed()
    {
        var admin = await api.AdminAsync();
        var role = (await admin.GetOkAsync("/roles")).EnumerateArray().First(r => r.GetProperty("key").GetString() == "Telemarketing");
        var roleId = role.GetProperty("id").GetInt32();
        var permissions = role.GetProperty("permissions").EnumerateArray()
            .Select(p => new { screen = p.GetProperty("screen").GetString()!, level = p.GetProperty("level").GetString()! }).ToList();
        var first = permissions[0];
        var flipped = permissions.Select(p => p == first ? p with { level = p.level == "Edit" ? "View" : "Edit" } : p).ToList();
        var before = (await admin.GetOkAsync($"/bitacora?entidad=Role&id={roleId}")).GetProperty("rowCount").GetInt32();

        await admin.PutOkAsync($"/roles/{roleId}/permisos", new { permissions = flipped });

        var log = await admin.GetOkAsync($"/bitacora?entidad=Role&id={roleId}");
        Assert.Equal(before + 1, log.GetProperty("rowCount").GetInt32());
        var entry = log.GetProperty("rows")[0];
        Assert.Equal("Permiso", entry.GetProperty("entityLabel").GetString());
        var level = Assert.Single(entry.GetProperty("changes").EnumerateArray());
        Assert.Equal("Nivel", level.GetProperty("label").GetString());
        Assert.Equal(first.level, level.GetProperty("before").GetString());

        await admin.PutOkAsync($"/roles/{roleId}/permisos", new { permissions });
    }
}
