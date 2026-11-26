using System.Net;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using CleanCom.Infrastructure.Persistence;
using CleanCom.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCom.IntegrationTests;

/// <summary>
/// Evidencias y caja (docs/pantallas/oficina/evidencias-y-caja.md) over a real SQL Server: a chofer's day with one
/// visit paid in cash and a photo far from the parada, the shift closed declaring less cash, and the office closing
/// the corte.
/// </summary>
[Collection("api")]
public class EvidenciasYCajaTests(ApiFactory api)
{
    [Fact]
    public async Task The_office_sees_the_visits_and_closes_the_corte_with_its_differences()
    {
        var admin = await api.AdminAsync();
        var (chofer, rutaId, today) = await DayAsync();
        await chofer.PostOkAsync("/chofer/turno/iniciar", new { km = 20_000 });
        await chofer.PostOkAsync("/chofer/turno/cerrar", new
        {
            km = 20_080,
            declared = new[] { new { method = "Efectivo", amount = 330m } },
            notes = "Di $20 de cambio de más",
        });

        // Visits: the cash visit with its photo marked outside the radius.
        var visitas = await admin.GetOkAsync($"/visitas?desde={today:yyyy-MM-dd}&hasta={today:yyyy-MM-dd}&ruta={rutaId}");
        var row = Assert.Single(visitas.GetProperty("rows").EnumerateArray());
        Assert.Equal("Efectivo", row.GetProperty("paymentMethod").GetString());
        Assert.Equal(350m, row.GetProperty("paymentAmount").GetDecimal());
        Assert.True(Assert.Single(row.GetProperty("photos").EnumerateArray()).GetProperty("outsideRadius").GetBoolean());
        var totals = visitas.GetProperty("totals");
        Assert.Equal(1, totals.GetProperty("done").GetInt32());
        Assert.Equal(350m, totals.GetProperty("collected").GetDecimal());
        Assert.Equal(1, totals.GetProperty("outsideRadius").GetInt32());
        var tarjeta = await admin.GetOkAsync($"/visitas?desde={today:yyyy-MM-dd}&ruta={rutaId}&formaPago=Tarjeta");
        Assert.Equal(0, tarjeta.GetProperty("rowCount").GetInt32());

        var excel = await admin.GetAsync($"/visitas/exportar?desde={today:yyyy-MM-dd}&ruta={rutaId}");
        Assert.Equal(HttpStatusCode.OK, excel.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excel.Content.Headers.ContentType?.MediaType);
        var bytes = await excel.Content.ReadAsByteArrayAsync();
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);

        // Corte: $350 recorded, $330 declared.
        var corte = await CorteAsync(admin, today);
        Assert.Equal("PorRevisar", corte.GetProperty("state").GetString());
        var efectivo = corte.GetProperty("lines").EnumerateArray().Single(l => l.GetProperty("method").GetString() == "Efectivo");
        Assert.Equal(-20m, efectivo.GetProperty("vsRegistered").GetDecimal());
        Assert.True(corte.GetProperty("hasDifference").GetBoolean());
        var corteId = corte.GetProperty("corteId").GetInt32();

        var pos = new[] { new { method = "Efectivo", amount = (decimal?)350m } };
        var withoutNote = await admin.PostJsonAsync($"/cortes/{corteId}/cerrar", new { pos, notes = (string?)null });
        Assert.Contains("anota qué pasó", (await withoutNote.ReadProblemAsync(HttpStatusCode.BadRequest)).GetProperty("detail").GetString());
        await admin.PostOkAsync($"/cortes/{corteId}/cerrar", new { pos, notes = "Se le descuentan $20." });

        corte = await CorteAsync(admin, today);
        Assert.Equal("Cerrado", corte.GetProperty("state").GetString());
        Assert.Equal($"CC-{today:yyyyMMdd}-CAJA", corte.GetProperty("folio").GetString());
        Assert.False(string.IsNullOrEmpty(corte.GetProperty("closedBy").GetString()));
        efectivo = corte.GetProperty("lines").EnumerateArray().Single(l => l.GetProperty("method").GetString() == "Efectivo");
        Assert.Equal(-20m, efectivo.GetProperty("vsPos").GetDecimal());
        var again = await admin.PostJsonAsync($"/cortes/{corteId}/cerrar", new { pos, notes = "otra vez" });
        await again.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    private static async Task<System.Text.Json.JsonElement> CorteAsync(HttpClient admin, DateOnly day) =>
        (await admin.GetOkAsync($"/cortes?fecha={day:yyyy-MM-dd}")).GetProperty("cortes").EnumerateArray()
            .Single(c => c.GetProperty("rutaCode").GetString() == "CAJA");

    /// <summary>
    /// Ruta CAJA today with its chofer and unidad, and one visit already closed: paid $350 in cash with a photo taken
    /// 450 m from the parada. The visit is written straight to the database: the chofer app is not the subject.
    /// </summary>
    private async Task<(HttpClient Chofer, int RutaId, DateOnly Today)> DayAsync()
    {
        var admin = await api.AdminAsync();
        var ruta = await admin.PostOkAsync("/rutas", new { code = "CAJA", name = "Ruta CAJA", color = "#8e24aa", cupoParadas = (int?)null, recoleccionGoal = (int?)null, maxReturnTime = (string?)null, maxReturnTimeSaturday = (string?)null, active = true });
        var rutaId = ruta.GetProperty("id").GetInt32();
        await admin.PostOkAsync("/usuarios", new
        {
            fullName = "Chofer Caja",
            userName = "chofer.caja",
            email = (string?)null,
            phone = (string?)null,
            role = "Chofer",
            initialPassword = "Chofer.Inicial2026",
            operadorId = (int?)null,
            createOperador = true,
            usualRutaId = rutaId,
        });
        var unidad = await admin.PostOkAsync("/unidades", new
        {
            plate = "NY9003B", brand = "Nissan", model = "NV200", year = 2023, garmentCapacity = (int?)null, km = 20_000,
            serviceEveryKm = (int?)null, serviceEveryMonths = (int?)null, state = "Activa", notes = (string?)null,
        });

        using var scope = api.Services.CreateScope();
        var today = scope.ServiceProvider.GetRequiredService<IBusinessClock>().Today;
        await admin.PutOkAsync($"/rutas/{rutaId}/dias/{today:yyyy-MM-dd}/unidad", new { unidadId = unidad.GetProperty("card").GetProperty("id").GetInt32(), confirm = true });
        var db = scope.ServiceProvider.GetRequiredService<CleanComDbContext>();
        var rutaDia = await db.RutaDias.SingleAsync(d => d.RutaId == rutaId && d.Date == today);
        var at = BusinessTime.ToUtc(today, new TimeOnly(0, 30));
        var parada = new Parada
        {
            RutaDiaId = rutaDia.Id,
            Kind = ParadaKind.Insumos,
            Title = "Bobinas a Leones",
            State = ParadaState.Realizada,
            EnCaminoAt = at,
            ArrivedAt = at,
            ClosedAt = at,
        };
        parada.Evidence.Add(new Evidence { Kind = EvidenceKind.Foto, FilePath = "evidencias/prueba.jpg", ContentType = "image/jpeg", SizeBytes = 10, TakenAt = at, DistanceMeters = 450 });
        db.Paradas.Add(parada);
        await db.SaveChangesAsync();
        db.ReferencePayments.Add(new ReferencePayment { ParadaId = parada.Id, Method = PaymentMethod.Efectivo, Amount = 350 });
        await db.SaveChangesAsync();

        var chofer = api.WithToken(await api.SignInOfficeAsync("chofer.caja", "Chofer.Inicial2026", "Chofer.Pruebas2026"));
        return (chofer, rutaId, today);
    }
}
