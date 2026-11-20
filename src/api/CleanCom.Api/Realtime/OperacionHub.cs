using CleanCom.Api.Common;
using CleanCom.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CleanCom.Api.Realtime;

/// <summary>
/// Hub "operacion" (/hubs/operacion): what changes during the day, pushed to the office (Panel del día and Rutas).
/// Events: "ubicacion" (<see cref="LiveLocationDto"/>) and "parada" (<see cref="ParadaChangedDto"/>). The browser
/// sends the token as access_token in the query string, since WebSockets cannot carry the Authorization header.
/// </summary>
[Authorize(Policy = Policies.SeesOperacion)]
public class OperacionHub : Hub
{
    public const string Path = "/hubs/operacion";
    public const string Office = "oficina";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, Office);
        await base.OnConnectedAsync();
    }
}

/// <summary>Sends the events to the office group. A failure is logged and never fails the chofer's request.</summary>
public class SignalROperacionNotifier(IHubContext<OperacionHub> hub, ILogger<SignalROperacionNotifier> logger) : IOperacionNotifier
{
    public Task LocationAsync(LiveLocationDto location, CancellationToken ct) => SendAsync("ubicacion", location, ct);

    public Task ParadaAsync(ParadaChangedDto change, CancellationToken ct) => SendAsync("parada", change, ct);

    private async Task SendAsync(string method, object payload, CancellationToken ct)
    {
        try
        {
            await hub.Clients.Group(OperacionHub.Office).SendAsync(method, payload, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not push {Method} to the office", method);
        }
    }
}
