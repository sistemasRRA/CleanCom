using System.Text.Json;
using CleanCom.Application.Avisos;
using CleanCom.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCom.IntegrationTests;

/// <summary>
/// The invitado asks for a service in the app and the office follows it: the same agenda engine as the web
/// (disponibilidad and agendar), the aviso to the invitado and the office bell, and the pedido's history.
/// </summary>
[Collection("api")]
public class AgendaFlowTests(ApiFactory api)
{
    [Fact]
    public async Task An_invitado_schedules_from_the_app_and_the_office_sees_it()
    {
        var scenario = await api.ScenarioAsync();
        var invitado = await api.InvitadoAsync("5530000101");
        var pedidoId = await ScheduleFromTheAppAsync(invitado);
        var admin = await api.AdminAsync();

        var pedido = await admin.GetOkAsync($"/pedidos/{pedidoId}");
        Assert.Equal("Agendado", pedido.GetProperty("card").GetProperty("etapa").GetString());
        Assert.Equal(scenario.RutaId, pedido.GetProperty("card").GetProperty("rutaId").GetInt32());

        // The aviso was recorded with the request; the job sends it (here, the test).
        await SendPendingAvisosAsync();
        var avisos = await admin.GetOkAsync($"/avisos?pedidoId={pedidoId}");
        var agendada = Assert.Single(avisos.GetProperty("avisos").EnumerateArray(), a => a.GetProperty("evento").GetString() == "RecoleccionAgendada");
        Assert.Equal("WhatsApp", agendada.GetProperty("canal").GetString());
        Assert.Equal("ParaMandarAMano", agendada.GetProperty("state").GetString());
        Assert.Contains("Lucía", agendada.GetProperty("text").GetString(), StringComparison.Ordinal);

        var bell = await admin.GetOkAsync("/avisos-oficina");
        Assert.Contains(bell.GetProperty("avisos").EnumerateArray(), a => a.GetProperty("kind").GetString() == "NuevoPedido" && a.GetProperty("pedidoId").GetInt32() == pedidoId);
    }

    [Fact]
    public async Task With_push_turned_off_the_aviso_goes_by_whatsapp_even_with_the_app()
    {
        await api.ScenarioAsync();
        var invitado = await api.InvitadoAsync("5530000105");
        await invitado.PostJsonAsync("/yo/dispositivos", new { token = "ExponentPushToken[prueba-preferencias]", platform = "android" });
        var howFoundUs = (await invitado.GetOkAsync("/motivos?type=HowFoundUs"))[0].GetProperty("text").GetString();
        var me = await invitado.PutOkAsync("/yo", new
        {
            name = "Sin Push", email = (string?)null, otherPhone = (string?)null, howFoundUs, receivesWhatsApp = true, receivesPush = false,
        });
        Assert.False(me.GetProperty("receivesPush").GetBoolean());
        Assert.False(me.GetProperty("receivesPromotions").GetBoolean());

        // Scheduling saves the profile again without the new fields, as the current app does: they stay.
        var pedidoId = await ScheduleFromTheAppAsync(invitado);
        Assert.False((await invitado.GetOkAsync("/yo")).GetProperty("receivesPush").GetBoolean());

        var admin = await api.AdminAsync();
        var avisos = await admin.GetOkAsync($"/avisos?pedidoId={pedidoId}");
        var agendada = Assert.Single(avisos.GetProperty("avisos").EnumerateArray(), a => a.GetProperty("evento").GetString() == "RecoleccionAgendada");
        Assert.Equal("WhatsApp", agendada.GetProperty("canal").GetString());
    }

    [Fact]
    public async Task Moving_a_pedido_by_hand_is_kept_in_its_history()
    {
        var invitado = await api.InvitadoAsync("5530000102");
        var pedidoId = await ScheduleFromTheAppAsync(invitado);
        var admin = await api.AdminAsync();

        var moved = await admin.PostOkAsync($"/pedidos/{pedidoId}/etapa", new { etapa = "Recolectado", reason = (string?)null });

        Assert.Equal("Recolectado", moved.GetProperty("card").GetProperty("etapa").GetString());
        Assert.Contains(moved.GetProperty("history").EnumerateArray(), h => h.GetProperty("text").GetString() == "Agendado → Recolectado (a mano)");
    }

    [Fact]
    public async Task The_invitado_sees_only_their_own_pedidos()
    {
        var ana = await api.InvitadoAsync("5530000103");
        var anaPedido = await ScheduleFromTheAppAsync(ana);
        var beto = await api.InvitadoAsync("5530000104");

        var mine = await beto.GetOkAsync("/yo/pedidos");
        var other = await beto.GetAsync($"/yo/pedidos/{anaPedido}/seguimiento");

        Assert.DoesNotContain(mine.EnumerateArray(), p => p.GetProperty("id").GetInt32() == anaPedido);
        Assert.False(other.IsSuccessStatusCode);
    }

    /// <summary>Profile, address in the scenario's zona, first available day and agendar, as the app does.</summary>
    private static async Task<int> ScheduleFromTheAppAsync(HttpClient invitado)
    {
        var howFoundUs = (await invitado.GetOkAsync("/motivos?type=HowFoundUs"))[0].GetProperty("text").GetString();
        await invitado.PutOkAsync("/yo", new { name = "Lucía Prueba", email = (string?)null, otherPhone = (string?)null, howFoundUs, receivesWhatsApp = true });
        var me = await invitado.PostOkAsync("/yo/direcciones", new
        {
            label = "Casa",
            street = "Calz. de Tlalpan",
            exteriorNumber = "1200",
            interiorNumber = (string?)null,
            postalCode = "03340",
            neighborhood = "Portales",
            borough = "Benito Juárez",
            references = (string?)null,
            lat = Scenario.Lat,
            lng = Scenario.Lng,
            zonaId = (int?)null,
        });
        var addressId = me.GetProperty("addresses")[0].GetProperty("id").GetInt32();
        var lineas = await invitado.GetOkAsync("/lineas-servicio");
        var linea = lineas.EnumerateArray().First(l => l.GetProperty("active").GetBoolean()).GetProperty("id").GetInt32();

        var availability = await invitado.PostOkAsync("/agenda/disponibilidad", new { addressId, turno = "CualquierHora" });
        Assert.False(availability.GetProperty("outOfCoverage").GetBoolean());
        var date = availability.GetProperty("date").GetString();
        Assert.NotNull(date);

        var scheduled = await invitado.PostOkAsync("/agenda/agendar", new
        {
            invitadoId = me.GetProperty("id").GetInt32(),
            addressId,
            turno = "CualquierHora",
            priority = false,
            lineaServicioIds = new[] { linea },
            date,
        });
        return scheduled.GetProperty("pedidoId").GetInt32();
    }

    private async Task SendPendingAvisosAsync()
    {
        using var scope = api.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AvisoDispatcher>().SendPendingAsync(CancellationToken.None);
    }
}

file static class JsonArray
{
    public static JsonElement First(this JsonElement.ArrayEnumerator items, Func<JsonElement, bool> predicate) => Enumerable.First(items, predicate);
}
