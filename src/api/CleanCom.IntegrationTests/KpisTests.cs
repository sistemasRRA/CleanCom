using System.Net;
using System.Text.Json;
using CleanCom.Domain.Kpis;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using CleanCom.Infrastructure.Persistence;
using CleanCom.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace CleanCom.IntegrationTests;

/// <summary>KPIs and their goals (docs/pantallas/oficina/kpis.md and whatsapp-y-metas.md) over a real SQL Server.</summary>
[Collection("api")]
public class KpisTests(ApiFactory api)
{
    [Fact]
    public async Task Every_indicator_is_calculated_with_its_detail_in_every_period()
    {
        var admin = await api.AdminAsync();
        await api.ScenarioAsync();

        foreach (var periodo in new[] { "Semana", "Mes", "Trimestre" })
        {
            var board = await admin.GetOkAsync($"/kpis?periodo={periodo}");
            Assert.Equal(KpiCatalog.All.Count, board.GetProperty("indicadores").GetArrayLength());
            Assert.True(board.GetProperty("isCurrent").GetBoolean());
        }
        foreach (var kpi in KpiCatalog.All)
        {
            var detail = await admin.GetOkAsync($"/kpis/{kpi.Key}/detalle?periodo=Mes");
            Assert.Equal(kpi.Name, detail.GetProperty("name").GetString());
        }
        var history = await admin.GetOkAsync("/kpis/historico?semanas=3");
        Assert.Equal(3, history.GetProperty("weeks").GetArrayLength());
        await admin.PostOkAsync("/kpis/recalcular?periodo=Semana");
    }

    [Fact]
    public async Task A_day_of_operation_is_counted_in_each_indicator()
    {
        var scenario = await api.ScenarioAsync();
        var admin = await api.AdminAsync();
        var (pedidoId, date) = await OperatedDayAsync(scenario);
        var query = $"periodo=Semana&fecha={date:yyyy-MM-dd}";

        async Task<JsonElement[]> RowsAsync(string key) =>
            [.. (await admin.GetOkAsync($"/kpis/{key}/detalle?{query}")).GetProperty("rows").EnumerateArray().Where(r => r.GetProperty("pedidoId").ValueKind == JsonValueKind.Number && r.GetProperty("pedidoId").GetInt32() == pedidoId)];

        Assert.Equal("Primer servicio", Assert.Single(await RowsAsync(KpiCatalog.ClientesNuevos)).GetProperty("text").GetString());
        Assert.Single(await RowsAsync(KpiCatalog.Recurrencia));
        Assert.Equal("Recolección realizada", Assert.Single(await RowsAsync(KpiCatalog.RecoleccionesPorRutaDia)).GetProperty("text").GetString());
        Assert.Equal("Entrega con firma", Assert.Single(await RowsAsync(KpiCatalog.TicketsFirmados)).GetProperty("text").GetString());
        Assert.Equal(2, (await RowsAsync(KpiCatalog.VisitasConFoto)).Length);
        Assert.Equal("Cobrado en la recolección", Assert.Single(await RowsAsync(KpiCatalog.CobroAnticipado)).GetProperty("text").GetString());
        Assert.Equal(480m, Assert.Single(await RowsAsync(KpiCatalog.FacturacionSinConvenios)).GetProperty("numerator").GetDecimal());
        Assert.Equal(6m, Assert.Single(await RowsAsync(KpiCatalog.PrendasPorTicket)).GetProperty("numerator").GetDecimal());

        var board = await admin.PostOkAsync($"/kpis/recalcular?{query}");
        var nuevos = board.GetProperty("indicadores").EnumerateArray().Single(c => c.GetProperty("key").GetString() == KpiCatalog.ClientesNuevos);
        Assert.True(nuevos.GetProperty("value").GetDecimal() >= 1);
    }

    /// <summary>
    /// Two days ago in the scenario's ruta: a new invitado's recolección (paid, with photo) and the entrega of the same
    /// pedido (with photo and firma), and its ticket. Written straight to the database: the chofer app is not the subject.
    /// </summary>
    private async Task<(int PedidoId, DateOnly Date)> OperatedDayAsync(Scenario scenario)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CleanComDbContext>();
        var date = scope.ServiceProvider.GetRequiredService<IBusinessClock>().Today.AddDays(-2);
        var at = BusinessTime.ToUtc(date, new TimeOnly(10, 0));
        var invitado = new Invitado { Name = "Nuevo KPI", WhatsAppPhone = "5530000199" };
        var address = new Address { Invitado = invitado, Street = "Calz. de Tlalpan", PostalCode = "03340", ZonaId = scenario.ZonaId, Location = new Point(Scenario.Lng, Scenario.Lat) { SRID = 4326 } };
        var pedido = new Pedido { Invitado = invitado, Address = address, Etapa = PedidoEtapa.Entregado, RegisteredAt = at, RecoleccionDate = date, EntregaDate = date };
        var rutaDia = await db.RutaDias.FirstOrDefaultAsync(d => d.RutaId == scenario.RutaId && d.Date == date) ?? new RutaDia { RutaId = scenario.RutaId, Date = date };
        Parada Visit(ParadaKind kind, int position) => new()
        {
            RutaDia = rutaDia, Kind = kind, Position = position, Pedido = pedido, Address = address, State = ParadaState.Realizada, ArrivedAt = at, ClosedAt = at,
            Evidence = [new Evidence { Kind = EvidenceKind.Foto, FilePath = "x.jpg", ContentType = "image/jpeg", TakenAt = at }],
        };
        var recoleccion = Visit(ParadaKind.Recoleccion, 1);
        var entrega = Visit(ParadaKind.Entrega, 2);
        entrega.Evidence.Add(new Evidence { Kind = EvidenceKind.Firma, FilePath = "x.png", ContentType = "image/png", TakenAt = at });
        db.Paradas.AddRange(recoleccion, entrega);
        db.Tickets.Add(new Ticket { Folio = "KPI-1", Pedido = pedido, LineaServicioId = (await db.LineasServicio.FirstAsync()).Id, Garments = 6, Amount = 480 });
        await db.SaveChangesAsync();
        db.ReferencePayments.Add(new ReferencePayment { ParadaId = recoleccion.Id, Method = PaymentMethod.Efectivo, Amount = 480 });
        // The audit interceptor stamps CreatedAt with the time of saving; the ticket counts by when it was captured.
        await db.SaveChangesAsync();
        await db.Tickets.Where(t => t.Folio == "KPI-1").ExecuteUpdateAsync(s => s.SetProperty(t => t.CreatedAt, at));
        return (pedido.Id, date);
    }

    [Fact]
    public async Task A_goal_applies_from_today_and_lights_the_indicator()
    {
        var admin = await api.AdminAsync();
        var metas = await admin.GetOkAsync("/metas");
        var responsable = metas.GetProperty("responsables")[0].GetProperty("id").GetInt32();

        // Toques a clientes por WhatsApp (more is better): with a goal, the card has a light and shows the responsable.
        await admin.PutOkAsync($"/metas/{KpiCatalog.ToquesWhatsApp}", new
        {
            period = "Semana",
            responsableUserId = responsable,
            red = 10,
            green = 20,
            superGreen = 30,
            validFrom = (DateOnly?)null,
        });

        var board = await admin.GetOkAsync("/kpis?periodo=Semana");
        var card = board.GetProperty("indicadores").EnumerateArray().Single(c => c.GetProperty("key").GetString() == KpiCatalog.ToquesWhatsApp);
        var goal = card.GetProperty("goal");
        Assert.Equal(responsable, goal.GetProperty("responsableUserId").GetInt32());
        Assert.NotEqual("SinMeta", card.GetProperty("light").GetString());

        var row = (await admin.GetOkAsync("/metas")).GetProperty("indicadores").EnumerateArray().Single(r => r.GetProperty("key").GetString() == KpiCatalog.ToquesWhatsApp);
        Assert.Equal(20m, row.GetProperty("current").GetProperty("green").GetDecimal());
    }

    [Fact]
    public async Task A_goal_per_ruta_lights_its_ruta_and_adds_up_to_the_company()
    {
        var scenario = await api.ScenarioAsync();
        var admin = await api.AdminAsync();

        // Only verde, per ruta: no rojo nor súper verde (optional).
        await admin.PutOkAsync($"/metas/{KpiCatalog.ClientesNuevos}", new
        {
            period = "Semana", responsableUserId = (int?)null, red = (decimal?)null, green = 6, superGreen = (decimal?)null,
            validFrom = (DateOnly?)null, rutaId = scenario.RutaId,
        });

        var metas = await admin.GetOkAsync("/metas");
        var row = metas.GetProperty("indicadores").EnumerateArray().Single(r => r.GetProperty("key").GetString() == KpiCatalog.ClientesNuevos);
        var ruta = row.GetProperty("rutas").EnumerateArray().Single(r => r.GetProperty("rutaId").GetInt32() == scenario.RutaId);
        Assert.Equal(6m, ruta.GetProperty("current").GetProperty("green").GetDecimal());
        Assert.Equal(JsonValueKind.Null, ruta.GetProperty("current").GetProperty("red").ValueKind);
        // Last week as the KPIs screen shows it, next to each goal.
        Assert.StartsWith("Semana del", metas.GetProperty("lastWeekLabel").GetString(), StringComparison.Ordinal);
        // The goal starts today, so last week keeps having none (vigencia).
        Assert.Equal("SinMeta", ruta.GetProperty("lastWeek").GetProperty("light").GetString());
        Assert.Equal(JsonValueKind.Number, ruta.GetProperty("lastWeek").GetProperty("value").ValueKind);

        var board = await admin.GetOkAsync("/kpis?periodo=Semana");
        var card = board.GetProperty("indicadores").EnumerateArray().Single(c => c.GetProperty("key").GetString() == KpiCatalog.ClientesNuevos);
        var byRuta = card.GetProperty("byRuta").EnumerateArray().Single(r => r.GetProperty("rutaId").GetInt32() == scenario.RutaId);
        Assert.NotEqual("SinMeta", byRuta.GetProperty("light").GetString());
        // The scenario's ruta is the only active one, so the company's goal is its goal (prorated to the days gone by).
        Assert.True(card.GetProperty("goal").GetProperty("fromRutas").GetBoolean());
    }

    [Fact]
    public async Task A_goal_cannot_start_in_the_past_nor_have_its_thresholds_out_of_order()
    {
        var admin = await api.AdminAsync();

        var past = await admin.PutJsonAsync($"/metas/{KpiCatalog.VisitasConFoto}", new
        {
            period = "Semana", responsableUserId = (int?)null, red = 80, green = 90, superGreen = 95, validFrom = new DateOnly(2020, 1, 1),
        });
        var disordered = await admin.PutJsonAsync($"/metas/{KpiCatalog.VisitasConFoto}", new
        {
            period = "Semana", responsableUserId = (int?)null, red = 95, green = 90, superGreen = 80, validFrom = (DateOnly?)null,
        });
        var unknown = await admin.GetAsync("/kpis/no-existe/detalle");

        await past.ReadProblemAsync(HttpStatusCode.BadRequest);
        var problem = await disordered.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Contains("rojo < verde < súper verde", problem.GetProperty("detail").GetString(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }
}
