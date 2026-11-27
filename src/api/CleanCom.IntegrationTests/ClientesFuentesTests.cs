using System.Text.Json;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
using CleanCom.Domain.Time;
using CleanCom.Infrastructure.Persistence;
using CleanCom.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCom.IntegrationTests;

/// <summary>
/// Clientes y fuentes (docs/pantallas/oficina/clientes-y-fuentes.md) over a real SQL Server: June 2026, already closed,
/// with one invitado of each kind from a source only this test uses.
/// </summary>
[Collection("api")]
public class ClientesFuentesTests(ApiFactory api)
{
    private const string Fuente = "Prueba Clientes y fuentes";

    [Fact]
    public async Task June_counts_each_invitado_once_by_source_and_sends_the_lost_to_call()
    {
        var admin = await api.AdminAsync();
        var ids = await JuneAsync();

        var junio = await admin.GetOkAsync("/gerencia/clientes?periodo=Mes&fecha=2026-06-15");
        Assert.Equal("Junio 2026", junio.GetProperty("label").GetString());
        Assert.False(junio.GetProperty("isPartial").GetBoolean());
        Assert.Equal(6, junio.GetProperty("trend").GetArrayLength());
        var row = junio.GetProperty("fuentes").EnumerateArray().Single(f => f.GetProperty("fuente").GetString() == Fuente);
        Assert.Equal(1, row.GetProperty("nuevos").GetInt32());
        Assert.Equal(300m, row.GetProperty("ventaNuevos").GetDecimal());
        // Nuevo, recurrente and recuperado; the perdido did not order in June.
        Assert.Equal(3, row.GetProperty("atendidos").GetInt32());
        Assert.Equal(3, row.GetProperty("pedidos").GetInt32());
        Assert.Equal(500m, row.GetProperty("venta").GetDecimal());
        Assert.Equal(250m, row.GetProperty("ticketPromedio").GetDecimal());
        Assert.Equal(1, row.GetProperty("perdidos").GetInt32());

        var perdidos = await admin.GetOkAsync($"/gerencia/clientes/perdidos?periodo=Mes&fecha=2026-06-15&fuente={Uri.EscapeDataString(Fuente)}");
        var perdido = Assert.Single(perdidos.GetProperty("rows").EnumerateArray());
        Assert.Equal(ids.Perdido, perdido.GetProperty("invitadoId").GetInt32());
        Assert.Equal("2026-06-02", perdido.GetProperty("lostOn").GetString());
        Assert.Equal("2026-02-01", perdido.GetProperty("lastService").GetString());

        var retencion = await admin.GetOkAsync($"/gerencia/clientes/retencion?fecha=2026-06-15&fuente={Uri.EscapeDataString(Fuente)}");
        var june = retencion.GetProperty("cohorts").EnumerateArray().Single(c => c.GetProperty("month").GetString() == "2026-06-01");
        Assert.Equal(1, june.GetProperty("nuevos").GetInt32());
        Assert.Equal(1, june.GetProperty("base30").GetInt32());
        Assert.Equal(0, june.GetProperty("repitieron30").GetInt32());

        // To "Llamar hoy" of the CRM, whatever their zona, until a contact is registered.
        var sent = await admin.PostOkAsync("/gerencia/clientes/perdidos/llamar", new { invitadoIds = new[] { ids.Perdido } });
        Assert.Equal(1, sent.GetProperty("sent").GetInt32());
        var llamar = await admin.GetOkAsync("/invitados/llamar-hoy?porPagina=200");
        var crmRow = llamar.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("id").GetInt32() == ids.Perdido);
        Assert.True(crmRow.GetProperty("reactivation").GetBoolean());
        await admin.PostOkAsync($"/invitados/{ids.Perdido}/contactos", new { channel = "Llamada", result = "NoContesto", note = (string?)null, nextContactOn = (string?)null });
        llamar = await admin.GetOkAsync("/invitados/llamar-hoy?porPagina=200");
        Assert.DoesNotContain(llamar.GetProperty("rows").EnumerateArray(), r => r.GetProperty("id").GetInt32() == ids.Perdido);
    }

    /// <summary>
    /// Nuevo (first service June 10, ticket $300), recurrente (May 1 and June 5, ticket $200), recuperado (January 10
    /// and June 20) and perdido (only February 1: more than 120 days on June 2). Written straight to the database.
    /// </summary>
    private async Task<(int Nuevo, int Perdido)> JuneAsync()
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CleanComDbContext>();
        var linea = (await db.LineasServicio.FirstAsync()).Id;
        var folio = 0;

        async Task<Invitado> InvitadoAsync(string name, string phone, string[] dates, decimal? amount)
        {
            var invitado = new Invitado { Name = name, WhatsAppPhone = phone, HowFoundUs = Fuente };
            var address = new Address { Invitado = invitado, Street = "Calle de prueba 1", PostalCode = "03340" };
            foreach (var d in dates.Select(DateOnly.Parse))
            {
                var pedido = new Pedido
                {
                    Invitado = invitado, Address = address, Etapa = PedidoEtapa.Entregado,
                    RegisteredAt = BusinessTime.ToUtc(d, new TimeOnly(9, 0)), RecoleccionDate = d,
                };
                db.Pedidos.Add(pedido);
                if (amount is { } a && d.Month == 6)
                {
                    db.Tickets.Add(new Ticket { Folio = $"CYF-{++folio}", Pedido = pedido, LineaServicioId = linea, Garments = 3, Amount = a });
                }
            }
            await db.SaveChangesAsync();
            return invitado;
        }

        var nuevo = await InvitadoAsync("Nuevo CyF", "5531000001", ["2026-06-10"], 300);
        await InvitadoAsync("Recurrente CyF", "5531000002", ["2026-05-01", "2026-06-05"], 200);
        await InvitadoAsync("Recuperado CyF", "5531000003", ["2026-01-10", "2026-06-20"], null);
        var perdido = await InvitadoAsync("Perdido CyF", "5531000004", ["2026-02-01"], null);
        // The audit interceptor stamps CreatedAt when saving; the tickets count by when they were captured (June).
        await db.Tickets.Where(t => t.Folio.StartsWith("CYF-"))
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.CreatedAt, BusinessTime.ToUtc(new DateOnly(2026, 6, 15), new TimeOnly(12, 0))));
        return (nuevo.Id, perdido.Id);
    }
}
