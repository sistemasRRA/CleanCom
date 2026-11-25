using CleanCom.Application.Common;
using CleanCom.Domain.Avisos;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanCom.Application.Avisos;

/// <summary>
/// Sends one channel's notices (Infrastructure): WhatsApp Cloud API, Expo push. Until the accounts exist each channel
/// has a test sender (<see cref="TestMode"/>): WhatsApp leaves the notice to send by hand and push only records it.
/// </summary>
public interface IAvisoSender
{
    AvisoCanal Canal { get; }

    /// <summary>True while it does not reach the provider (no account or keys yet).</summary>
    bool TestMode { get; }

    Task<AvisoSendResult> SendAsync(AvisoMessage message, CancellationToken ct);
}

/// <summary>
/// The outbox: sends the pending notices with each channel's sender (a background job calls it). A WhatsApp notice
/// that cannot be sent stays to send by hand, as the current system does.
/// </summary>
public class AvisoDispatcher(ICleanComDbContext db, IEnumerable<IAvisoSender> senders, IBusinessClock clock, ILogger<AvisoDispatcher> logger)
{
    public const int Batch = 50;

    /// <summary>Sends up to <see cref="Batch"/> pending notices, oldest first. Returns how many it handled.</summary>
    public async Task<int> SendPendingAsync(CancellationToken ct)
    {
        var pending = await db.Avisos.Where(a => a.State == AvisoState.Pendiente).OrderBy(a => a.Id).Take(Batch).ToListAsync(ct);
        foreach (var aviso in pending)
        {
            aviso.Attempts++;
            var result = await SendAsync(aviso, ct);
            aviso.State = result.State;
            aviso.Detail = result.Detail;
            if (result.State is AvisoState.Enviado or AvisoState.Simulado)
            {
                aviso.SentAt = clock.UtcNow;
            }
            await db.SaveChangesAsync(ct);
        }
        return pending.Count;
    }

    private async Task<AvisoSendResult> SendAsync(Aviso aviso, CancellationToken ct)
    {
        var sender = senders.FirstOrDefault(s => s.Canal == aviso.Canal);
        if (sender is null || aviso.Canal is not { } canal)
        {
            return Failed(aviso.Canal, "No hay remitente para este canal.");
        }
        try
        {
            var tokens = canal == AvisoCanal.Push
                ? await db.DispositivosPush.Where(d => d.InvitadoId == aviso.InvitadoId && d.Active).Select(d => d.Token).ToListAsync(ct)
                : [];
            var values = aviso.Values.Length == 0 ? [] : aviso.Values.Split(AvisoRules.ValueSeparator);
            return await sender.SendAsync(new AvisoMessage(aviso.Id, canal, aviso.Phone, aviso.MetaName, values, aviso.Title, aviso.Text, tokens), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not send aviso {AvisoId} by {Canal}", aviso.Id, canal);
            return Failed(canal, "No se pudo enviar.");
        }
    }

    private static AvisoSendResult Failed(AvisoCanal? canal, string detail) =>
        canal == AvisoCanal.WhatsApp ? new AvisoSendResult(AvisoState.ParaMandarAMano, $"{detail} Mándalo a mano.") : new AvisoSendResult(AvisoState.Fallido, detail);
}
