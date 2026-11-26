using System.Net;
using System.Text.Json;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using CleanCom.Infrastructure.Persistence;
using CleanCom.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCom.IntegrationTests;

/// <summary>
/// Inicio and fin de turno and fallas of the unidad in the chofer app (docs/pantallas/apps/chofer.md) over a real
/// SQL Server: a chofer of their own ruta and a unidad the office assigns to it today.
/// </summary>
[Collection("api")]
public class ChoferShiftTests(ApiFactory api)
{
    [Fact]
    public async Task A_shift_records_the_km_of_the_unidad_and_the_declared_payments()
    {
        var (chofer, rutaId, unidadId, plate) = await ChoferAsync("TURNO", "chofer.turno", "NY9001B", 45_200);
        var admin = await api.AdminAsync();

        // The unidad is always the office's: without it the shift does not start.
        var shift = (await chofer.GetOkAsync("/chofer/ruta")).GetProperty("shift");
        Assert.Equal(JsonValueKind.Null, shift.GetProperty("unidad").ValueKind);
        var missing = await chofer.PostJsonAsync("/chofer/turno/iniciar", new { km = 45_210 });
        Assert.Contains("Oficina no te ha asignado unidad", (await missing.ReadProblemAsync(HttpStatusCode.BadRequest)).GetProperty("detail").GetString());
        var paradaId = await AssignAndAddParadaAsync(rutaId, unidadId);
        shift = (await chofer.GetOkAsync("/chofer/ruta")).GetProperty("shift");
        Assert.Equal(plate, shift.GetProperty("unidad").GetProperty("plate").GetString());

        // The shift is required before working the paradas.
        var early = await chofer.PostJsonAsync($"/chofer/paradas/{paradaId}/en-camino", new { lat = (double?)null, lng = (double?)null });
        Assert.Contains("Inicia tu turno", (await early.ReadProblemAsync(HttpStatusCode.BadRequest)).GetProperty("detail").GetString());

        // Lower than the unidad is rejected; one digit more asks to confirm.
        var lower = await chofer.PostJsonAsync("/chofer/turno/iniciar", new { km = 45_150 });
        Assert.Contains("45,200 km", (await lower.ReadProblemAsync(HttpStatusCode.BadRequest)).GetProperty("detail").GetString());
        var typo = await chofer.PostJsonAsync("/chofer/turno/iniciar", new { km = 452_100 });
        await typo.ReadProblemAsync(HttpStatusCode.Conflict);

        var started = await chofer.PostOkAsync("/chofer/turno/iniciar", new { km = 45_210, localId = "turno-inicio-1" });
        shift = started.GetProperty("shift");
        Assert.Equal(45_210, shift.GetProperty("startKm").GetInt32());
        Assert.Equal($"Turno iniciado con la unidad {plate}.", started.GetProperty("message").GetString());
        // A resend after losing signal is answered without an error; starting again is not.
        await chofer.PostOkAsync("/chofer/turno/iniciar", new { km = 45_210, localId = "turno-inicio-1" });
        var again = await chofer.PostJsonAsync("/chofer/turno/iniciar", new { km = 45_220, localId = "turno-inicio-2" });
        Assert.Contains("Ya iniciaste tu turno", (await again.ReadProblemAsync(HttpStatusCode.BadRequest)).GetProperty("detail").GetString());
        Assert.Equal(45_210, (await admin.GetOkAsync($"/unidades/{unidadId}")).GetProperty("card").GetProperty("km").GetInt32());
        await chofer.PostOkAsync($"/chofer/paradas/{paradaId}/en-camino", new { lat = (double?)null, lng = (double?)null });

        var declared = new[] { new { method = "Efectivo", amount = 350m }, new { method = "Tarjeta", amount = 0m } };
        var below = await chofer.PostJsonAsync("/chofer/turno/cerrar", new { km = 45_200, declared });
        await below.ReadProblemAsync(HttpStatusCode.BadRequest);
        // A long shift and the parada still open are asked together.
        var longShift = await chofer.PostJsonAsync("/chofer/turno/cerrar", new { km = 45_700, declared });
        var question = (await longShift.ReadProblemAsync(HttpStatusCode.Conflict)).GetProperty("detail").GetString();
        Assert.Contains("Recorriste 490 km", question);
        Assert.Contains("Te queda 1 parada sin cerrar", question);

        var ended = await chofer.PostOkAsync("/chofer/turno/cerrar", new { km = 45_700, declared, notes = "Faltan $20 de cambio", confirm = true, localId = "turno-fin-1" });
        shift = ended.GetProperty("shift");
        Assert.Equal(45_700, shift.GetProperty("endKm").GetInt32());
        var efectivo = shift.GetProperty("declared").EnumerateArray().Single(d => d.GetProperty("method").GetString() == "Efectivo");
        Assert.Equal(350m, efectivo.GetProperty("amount").GetDecimal());
        await chofer.PostOkAsync("/chofer/turno/cerrar", new { km = 45_700, declared, confirm = true, localId = "turno-fin-1" });
        Assert.Equal(45_700, (await admin.GetOkAsync($"/unidades/{unidadId}")).GetProperty("card").GetProperty("km").GetInt32());
    }

    [Fact]
    public async Task A_falla_reported_from_the_app_stays_open_in_Unidades_once()
    {
        var (chofer, rutaId, unidadId, _) = await ChoferAsync("FALLA", "chofer.falla", "NY9002B", 10_000);
        var admin = await api.AdminAsync();

        // Before the office assigns a unidad there is nothing to report on.
        var early = await chofer.PostAsync("/chofer/fallas", Form("No enciende la luz de freno", "Media", "falla-0"));
        await early.ReadProblemAsync(HttpStatusCode.BadRequest);

        await AssignAndAddParadaAsync(rutaId, unidadId);
        await chofer.PostOkAsync("/chofer/turno/iniciar", new { km = 10_050 });
        var reported = await (await chofer.PostAsync("/chofer/fallas", Form("No enciende la luz de freno", "Media", "falla-1"))).ReadOkAsync();
        Assert.Contains("Falla reportada", reported.GetProperty("message").GetString());
        await (await chofer.PostAsync("/chofer/fallas", Form("No enciende la luz de freno", "Media", "falla-1"))).ReadOkAsync();

        var unidad = await admin.GetOkAsync($"/unidades/{unidadId}");
        var falla = Assert.Single(unidad.GetProperty("events").EnumerateArray(), e => e.GetProperty("kind").GetString() == "Falla");
        Assert.StartsWith("No enciende la luz de freno (reportó", falla.GetProperty("description").GetString());
        Assert.Equal(1, unidad.GetProperty("card").GetProperty("openFailures").GetInt32());
        // The chofer sees it as a warning on the unidad of the day.
        var shift = (await chofer.GetOkAsync("/chofer/ruta")).GetProperty("shift");
        Assert.Contains(shift.GetProperty("unidad").GetProperty("issues").EnumerateArray(), i => i.GetString()!.Contains("Falla abierta"));
    }

    private static MultipartFormDataContent Form(string description, string severity, string localId) => new()
    {
        { new StringContent(description), "Description" },
        { new StringContent(severity), "Severity" },
        { new StringContent(localId), "LocalId" },
    };

    /// <summary>
    /// The office assigns the unidad to the ruta today (Rutas), and the ruta gets one parada of insumos (written
    /// straight to the database: the agenda is not the subject). Returns the parada.
    /// </summary>
    private async Task<int> AssignAndAddParadaAsync(int rutaId, int unidadId)
    {
        var admin = await api.AdminAsync();
        using var scope = api.Services.CreateScope();
        var today = scope.ServiceProvider.GetRequiredService<IBusinessClock>().Today;
        await admin.PutOkAsync($"/rutas/{rutaId}/dias/{today:yyyy-MM-dd}/unidad", new { unidadId, confirm = true });
        var db = scope.ServiceProvider.GetRequiredService<CleanComDbContext>();
        var rutaDia = await db.RutaDias.SingleAsync(d => d.RutaId == rutaId && d.Date == today);
        var parada = new Parada { RutaDiaId = rutaDia.Id, Kind = ParadaKind.Insumos, Title = "Bobinas a Leones", AddressText = "Calz. de los Leones 165" };
        db.Paradas.Add(parada);
        await db.SaveChangesAsync();
        return parada.Id;
    }

    /// <summary>A ruta of its own, a chofer whose usual ruta it is and a unidad with its km.</summary>
    private async Task<(HttpClient Chofer, int RutaId, int UnidadId, string Plate)> ChoferAsync(string rutaCode, string userName, string plate, int km)
    {
        var admin = await api.AdminAsync();
        var ruta = await admin.PostOkAsync("/rutas", new { code = rutaCode, name = $"Ruta {rutaCode}", color = "#43a047", cupoParadas = (int?)null, recoleccionGoal = (int?)null, maxReturnTime = (string?)null, maxReturnTimeSaturday = (string?)null, active = true });
        await admin.PostOkAsync("/usuarios", new
        {
            fullName = $"Chofer {rutaCode}",
            userName,
            email = (string?)null,
            phone = (string?)null,
            role = "Chofer",
            initialPassword = "Chofer.Inicial2026",
            operadorId = (int?)null,
            createOperador = true,
            usualRutaId = ruta.GetProperty("id").GetInt32(),
        });
        var unidad = await admin.PostOkAsync("/unidades", new
        {
            plate,
            brand = "Nissan",
            model = "NV200",
            year = 2023,
            garmentCapacity = (int?)null,
            km,
            serviceEveryKm = (int?)null,
            serviceEveryMonths = (int?)null,
            state = "Activa",
            notes = (string?)null,
        });
        var chofer = api.WithToken(await api.SignInOfficeAsync(userName, "Chofer.Inicial2026", "Chofer.Pruebas2026"));
        return (chofer, ruta.GetProperty("id").GetInt32(), unidad.GetProperty("card").GetProperty("id").GetInt32(), plate);
    }
}
