using CleanCom.Application.Avisos;
using CleanCom.Domain.Avisos;
using Microsoft.Extensions.Logging;

namespace CleanCom.Infrastructure.Messaging;

/// <summary>
/// WhatsApp while there is no Meta account (docs/04-integraciones.md, WhatsApp): nothing goes out; the notice stays
/// for the office to send by hand with one click (WhatsApp y metas › Por mandar), as the current system does.
/// </summary>
public class TestWhatsAppSender(ILogger<TestWhatsAppSender> logger) : IAvisoSender
{
    public AvisoCanal Canal => AvisoCanal.WhatsApp;
    public bool TestMode => true;

    public Task<AvisoSendResult> SendAsync(AvisoMessage message, CancellationToken ct)
    {
        logger.LogInformation("Aviso {AvisoId} ({MetaName}) left to send by hand: WhatsApp is not connected", message.AvisoId, message.MetaName);
        return Task.FromResult(new AvisoSendResult(AvisoState.ParaMandarAMano, "WhatsApp no está conectado: mándalo a mano."));
    }
}

/// <summary>Push while Expo push is not set up for the app (EAS project): the notice is only recorded.</summary>
public class TestPushSender(ILogger<TestPushSender> logger) : IAvisoSender
{
    public AvisoCanal Canal => AvisoCanal.Push;
    public bool TestMode => true;

    public Task<AvisoSendResult> SendAsync(AvisoMessage message, CancellationToken ct)
    {
        logger.LogInformation("Aviso {AvisoId} by push to {Devices} device(s), simulated: {Title} · {Text}", message.AvisoId, message.Tokens.Count,
            message.Title, message.Text);
        return Task.FromResult(new AvisoSendResult(AvisoState.Simulado, $"Modo de prueba: no se envió ({message.Tokens.Count} dispositivo(s))."));
    }
}
