using System.Security.Cryptography;
using System.Text;
using CleanCom.Domain.Common;

namespace CleanCom.Domain.Access;

/// <summary>
/// A sign-in code for the invitado app (docs/pantallas/apps/invitado.md, Registro y acceso), sent by WhatsApp. Only
/// its hash is stored. A code is used once; asking for a new one leaves the earlier ones unusable.
/// </summary>
public class AccessCode : Entity
{
    /// <summary>10 digits (Phones.Normalize).</summary>
    public string Phone { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public int Attempts { get; set; }
    /// <summary>When it was used or replaced by a newer code; null while it can still be used.</summary>
    public DateTimeOffset? ClosedAt { get; set; }
}

/// <summary>Rules of the sign-in codes; the times and limits come from Acceso.* settings.</summary>
public static class AccessCodes
{
    public const int Length = 6;

    /// <summary>Six random digits, from a cryptographic generator.</summary>
    public static string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    /// <summary>SHA-256 of phone and code, so the same code for two phones gives two different hashes.</summary>
    public static string Hash(string phone, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{phone}:{code.Trim()}")));

    /// <summary>Compares in constant time.</summary>
    public static bool Matches(AccessCode stored, string phone, string code) =>
        CryptographicOperations.FixedTimeEquals(Convert.FromHexString(stored.CodeHash), Convert.FromHexString(Hash(phone, code)));

    /// <summary>
    /// Throws, in Spanish, when a new code cannot be sent yet: less than Acceso.CodigoReenvioSegundos since the last
    /// one, or Acceso.CodigosPorHora already sent in the last hour.
    /// </summary>
    public static void EnsureCanSend(IReadOnlyList<DateTimeOffset> sentInLastHour, DateTimeOffset now, int resendSeconds, int perHour)
    {
        if (sentInLastHour.Count > 0)
        {
            var wait = resendSeconds - (int)(now - sentInLastHour.Max()).TotalSeconds;
            if (wait > 0)
            {
                throw new DomainException($"Espera {wait} segundos para pedir otro código.");
            }
        }
        if (sentInLastHour.Count >= perHour)
        {
            throw new DomainException("Pediste varios códigos seguidos. Intenta de nuevo en una hora.");
        }
    }

    /// <summary>
    /// Checks a code the invitado typed against the open one: it exists, has not expired, has attempts left and
    /// matches. Counts the attempt and closes the code when it is used or out of attempts.
    /// </summary>
    public static void Verify(AccessCode? open, string phone, string code, DateTimeOffset now, int maxAttempts)
    {
        if (open is null || open.ClosedAt is not null || open.ExpiresAt <= now)
        {
            throw new DomainException("El código venció. Pide uno nuevo.");
        }
        open.Attempts++;
        if (code.Trim().Length != Length || !code.Trim().All(char.IsAsciiDigit) || !Matches(open, phone, code))
        {
            if (open.Attempts >= maxAttempts)
            {
                open.ClosedAt = now;
                throw new DomainException("El código no es correcto y ya no quedan intentos. Pide uno nuevo.");
            }
            var left = maxAttempts - open.Attempts;
            throw new DomainException($"El código no es correcto. Te queda{(left == 1 ? "" : "n")} {left} intento{(left == 1 ? "" : "s")}.");
        }
        open.ClosedAt = now;
    }

    /// <summary>"55 **** 5678": enough to recognize the number without showing it whole.</summary>
    public static string Mask(string phone) => phone.Length == 10 ? $"{phone[..2]} **** {phone[6..]}" : phone;
}
