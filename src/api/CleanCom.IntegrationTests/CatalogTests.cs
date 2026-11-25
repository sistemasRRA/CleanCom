using System.Net;
using CleanCom.IntegrationTests.Infrastructure;

namespace CleanCom.IntegrationTests;

/// <summary>Catalogs of Operación: nothing is deleted, "eliminar" is a logical deactivation (CLAUDE.md).</summary>
[Collection("api")]
public class CatalogTests(ApiFactory api)
{
    [Fact]
    public async Task Deleting_a_lugar_deactivates_it_and_keeps_it_in_the_database()
    {
        var admin = await api.AdminAsync();
        var lugar = await admin.PostOkAsync("/lugares", new { name = "Bodega de pruebas", type = "Bodega", address = "Calz. de Tlalpan 1200", lat = 19.37, lng = -99.17, openingHours = (string?)null, active = true });
        var id = lugar.GetProperty("id").GetInt32();

        var deleted = await admin.DeleteAsync($"/lugares/{id}");
        var active = await admin.GetOkAsync("/lugares");
        var all = await admin.GetOkAsync("/lugares?includeInactive=true");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.DoesNotContain(active.EnumerateArray(), l => l.GetProperty("id").GetInt32() == id);
        var kept = Assert.Single(all.EnumerateArray(), l => l.GetProperty("id").GetInt32() == id);
        Assert.False(kept.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task A_repeated_ruta_code_is_rejected_in_Spanish()
    {
        var admin = await api.AdminAsync();
        await api.ScenarioAsync();

        var response = await admin.PostJsonAsync("/rutas", new { code = "T", name = "Otra", color = "#000000", active = true });

        var problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Contains("T", problem.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }
}
