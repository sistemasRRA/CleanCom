using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using System.Net.Http.Headers;
using System.Text.Json;
using CleanCom.Application.Common;
using CleanCom.Infrastructure.Persistence;
using CleanCom.Jobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CleanCom.IntegrationTests.Infrastructure;

/// <summary>
/// The real API (Program) over a throwaway SQL Server database with every migration applied. Only two things are
/// replaced: the sign-in code is captured instead of sent, and the background jobs do not run (tests send the avisos
/// themselves, so results do not depend on timing). Shared by every test class of the "api" collection.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminUser = "admin.pruebas";
    private const string AdminInitialPassword = "Inicial.Pruebas2026";
    private const string AdminPassword = "Pruebas.2026!x";

    private readonly TestDatabase database = new();
    private readonly SemaphoreSlim adminLock = new(1, 1);
    private readonly SemaphoreSlim scenarioLock = new(1, 1);
    private string? adminToken;
    private Scenario? scenario;

    public CapturingAccessCodeSender AccessCodes { get; } = new();

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        var options = new DbContextOptionsBuilder<CleanComDbContext>()
            .UseSqlServer(database.ConnectionString, sql => sql
                .UseNetTopologySuite()
                .MigrationsAssembly("CleanCom.Migrations")
                .MigrationsHistoryTable("__ef_migrations_history"))
            .Options;
        await using (var db = new CleanComDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        // Program reads these while registering services, before the test host's settings apply: environment variables.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", database.ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Key", Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
        Environment.SetEnvironmentVariable("Seed__Admin__UserName", AdminUser);
        Environment.SetEnvironmentVariable("Seed__Admin__Password", AdminInitialPassword);
        Environment.SetEnvironmentVariable("AccessCodes__LogToConsole", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccessCodeSender>();
            services.AddSingleton<IAccessCodeSender>(AccessCodes);
            foreach (var job in services.Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Assembly == typeof(AvisosJob).Assembly).ToList())
            {
                services.Remove(job);
            }
            services.AddSingleton<IStartupFilter, TestClientIpFilter>();
        });
    }

    /// <summary>Header with the client IP a test request comes from (the test server has none).</summary>
    public const string ClientIpHeader = "X-Test-Client-Ip";

    /// <summary>
    /// The sign-in code is limited per IP and minute, and every test request comes from the same one: each invitado of
    /// the tests signs in from its own IP so the suite does not hit the limit as it grows. The limit itself is unchanged.
    /// </summary>
    private sealed class TestClientIpFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(ClientIpHeader, out var ip) && IPAddress.TryParse(ip, out var address))
                {
                    context.Connection.RemoteIpAddress = address;
                }
                return nextMiddleware(context);
            });
            next(app);
        };
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await database.DisposeAsync();
        adminLock.Dispose();
        scenarioLock.Dispose();
    }

    /// <summary>A client without session.</summary>
    public HttpClient Anonymous() => CreateClient();

    /// <summary>A client with the given access token.</summary>
    public HttpClient WithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>The seeded SuperUser; the first time it changes the initial password, as the office does.</summary>
    public async Task<HttpClient> AdminAsync()
    {
        await adminLock.WaitAsync();
        try
        {
            adminToken ??= await SignInOfficeAsync(AdminUser, AdminInitialPassword, AdminPassword);
            return WithToken(adminToken);
        }
        finally
        {
            adminLock.Release();
        }
    }

    /// <summary>Office sign-in; when the password is initial or temporary, it is changed to <paramref name="newPassword"/>.</summary>
    public async Task<string> SignInOfficeAsync(string userName, string password, string newPassword)
    {
        var session = await Anonymous().PostOkAsync("/auth/login", new { userName, password });
        if (!session.GetProperty("user").GetProperty("mustChangePassword").GetBoolean()) return session.Token();
        session = await WithToken(session.Token()).PostOkAsync("/auth/cambiar-contrasena", new { currentPassword = password, newPassword });
        return session.Token();
    }

    /// <summary>The invitado app's sign-in: asks for the code, reads it as WhatsApp would deliver it and verifies it.</summary>
    public async Task<HttpClient> InvitadoAsync(string phone)
    {
        var client = Anonymous();
        // Its own IP per phone (10.x.y.z from the last digits), see TestClientIpFilter.
        var n = int.Parse(phone[^6..]);
        client.DefaultRequestHeaders.Add(ClientIpHeader, $"10.{n / 65536 % 256}.{n / 256 % 256}.{n % 256}");
        await client.PostOkAsync("/auth/codigo", new { phone });
        var verified = await client.PostOkAsync("/auth/verificar", new { phone, code = AccessCodes.CodeFor(phone) });
        return WithToken(verified.GetProperty("session").Token());
    }

    /// <summary>
    /// A ruta and its zona núcleo (a square around Portales, Benito Juárez) visited every day, created once and shared:
    /// addresses at <see cref="Scenario.Lat"/>, <see cref="Scenario.Lng"/> fall in it.
    /// </summary>
    public async Task<Scenario> ScenarioAsync()
    {
        await scenarioLock.WaitAsync();
        try
        {
            if (scenario is not null) return scenario;
            var admin = await AdminAsync();
            var ruta = await admin.PostOkAsync("/rutas", new { code = "T", name = "Ruta de pruebas", color = "#1e88e5", cupoParadas = (int?)null, recoleccionGoal = (int?)null, maxReturnTime = (string?)null, maxReturnTimeSaturday = (string?)null, active = true });
            var rutaId = ruta.GetProperty("id").GetInt32();
            var zona = await admin.PostOkAsync("/zonas", new
            {
                code = "T1",
                rutaId,
                zonaNucleoId = (int?)null,
                active = true,
                polygon = new[] { new { lat = 19.39, lng = -99.19 }, new { lat = 19.39, lng = -99.15 }, new { lat = 19.35, lng = -99.15 }, new { lat = 19.35, lng = -99.19 } },
            });
            var zonaId = zona.GetProperty("id").GetInt32();
            await admin.PutOkAsync($"/zonas/{zonaId}/dias", new { days = Enum.GetNames<DayOfWeek>() });
            scenario = new Scenario(rutaId, zonaId);
            return scenario;
        }
        finally
        {
            scenarioLock.Release();
        }
    }
}

public sealed record Scenario(int RutaId, int ZonaId)
{
    public const double Lat = 19.37;
    public const double Lng = -99.17;
}

/// <summary>Keeps the last sign-in code per phone instead of sending it.</summary>
public sealed class CapturingAccessCodeSender : IAccessCodeSender
{
    private readonly ConcurrentDictionary<string, string> codes = new();

    public Task SendAsync(string phone, string code, int minutes, CancellationToken ct)
    {
        codes[phone] = code;
        return Task.CompletedTask;
    }

    public string CodeFor(string phone) =>
        codes.TryGetValue(phone, out var code)
            ? code
            : codes.FirstOrDefault(c => c.Key.EndsWith(phone, StringComparison.Ordinal)).Value ?? throw new InvalidOperationException($"No code was sent to {phone}.");
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>;

file static class SessionJson
{
    public static string Token(this JsonElement session) => session.GetProperty("accessToken").GetString()!;
}
