using CleanCom.Application.Common;
using CleanCom.Domain.Access;
using CleanCom.Domain.Common;
using Microsoft.Extensions.Logging;

namespace CleanCom.Infrastructure.Messaging;

/// <summary>
/// Development only (AccessCodes:LogToConsole): writes the sign-in code to the API log instead of sending it, so the
/// invitado app can be tried before WhatsApp and Twilio are connected (week 6).
/// </summary>
public class LoggingAccessCodeSender(ILogger<LoggingAccessCodeSender> logger) : IAccessCodeSender
{
    public Task SendAsync(string phone, string code, int minutes, CancellationToken ct)
    {
        logger.LogWarning("Sign-in code for {Phone}: {Code} (valid {Minutes} min; development, not sent)", AccessCodes.Mask(phone), code, minutes);
        return Task.CompletedTask;
    }
}

/// <summary>Anywhere else, until WhatsApp is connected: says so instead of pretending the code was sent.</summary>
public class UnavailableAccessCodeSender : IAccessCodeSender
{
    public Task SendAsync(string phone, string code, int minutes, CancellationToken ct) =>
        throw new DomainException("El acceso con celular todavía no está disponible. Escríbenos por WhatsApp para agendar.");
}
